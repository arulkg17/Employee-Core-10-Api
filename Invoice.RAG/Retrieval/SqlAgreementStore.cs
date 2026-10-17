using Dapper;
using Invoice.RAG.Models;
using Microsoft.Data.SqlClient;

namespace Invoice.RAG.Retrieval;

/// <summary>
/// SQL Server store (run Sql/001_rag_tables.sql first). Embeddings are kept as VARBINARY and compared in memory,
/// which is fine for thousands of chunks and needs no vector extension.
/// </summary>
public sealed class SqlAgreementStore(IConfiguration configuration) : IAgreementStore
{
    private string ConnectionString =>
        configuration.GetConnectionString("RagDb")
        ?? throw new InvalidOperationException("ConnectionStrings:RagDb is not configured.");

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    public async Task<int> AddAgreementAsync(AgreementDocument doc, IReadOnlyList<AgreementChunk> chunks, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var id = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"INSERT INTO dbo.VendorAgreement
                (VendorId, Title, FileName, StoredPath, Sha256, EffectiveDate, PageCount, ChunkCount, UploadedBy, UploadedUtc)
              OUTPUT INSERTED.Id
              VALUES (@VendorId, @Title, @FileName, @StoredPath, @Sha256, @EffectiveDate, @PageCount, @ChunkCount, @UploadedBy, @UploadedUtc)",
            new
            {
                doc.VendorId, doc.Title, doc.FileName, doc.StoredPath, doc.Sha256,
                EffectiveDate = doc.EffectiveDate?.ToDateTime(TimeOnly.MinValue),
                doc.PageCount, doc.ChunkCount, doc.UploadedBy, doc.UploadedUtc
            },
            tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO dbo.VendorAgreementChunk (AgreementId, ChunkIndex, Page, Section, Content, Embedding)
              VALUES (@AgreementId, @ChunkIndex, @Page, @Section, @Content, @Embedding)",
            chunks.Select(c => new
            {
                AgreementId = id, c.ChunkIndex, c.Page, c.Section, c.Content,
                Embedding = VectorMath.ToBytes(c.Embedding)
            }).ToList(),
            tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        doc.Id = id;
        return id;
    }

    public async Task<AgreementDocument?> FindByHashAsync(int vendorId, string sha256, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<AgreementRow>(new CommandDefinition(
            "SELECT * FROM dbo.VendorAgreement WHERE VendorId = @vendorId AND Sha256 = @sha256",
            new { vendorId, sha256 }, cancellationToken: ct));
        return row?.ToModel();
    }

    public async Task<AgreementDocument?> GetAsync(int id, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<AgreementRow>(new CommandDefinition(
            "SELECT * FROM dbo.VendorAgreement WHERE Id = @id", new { id }, cancellationToken: ct));
        return row?.ToModel();
    }

    public async Task<IReadOnlyList<AgreementDocument>> ListAsync(int? vendorId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        var rows = await conn.QueryAsync<AgreementRow>(new CommandDefinition(
            @"SELECT * FROM dbo.VendorAgreement
              WHERE (@vendorId IS NULL OR VendorId = @vendorId)
              ORDER BY UploadedUtc DESC, Id DESC",
            new { vendorId }, cancellationToken: ct));
        return rows.Select(r => r.ToModel()).ToList();
    }

    public async Task<IReadOnlyList<AgreementChunk>> GetChunksAsync(int? agreementId, int? vendorId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        var rows = await conn.QueryAsync<ChunkRow>(new CommandDefinition(
            @"SELECT c.Id, c.AgreementId, c.ChunkIndex, c.Page, c.Section, c.Content, c.Embedding
              FROM dbo.VendorAgreementChunk c
              JOIN dbo.VendorAgreement a ON a.Id = c.AgreementId
              WHERE (@agreementId IS NULL OR a.Id = @agreementId)
                AND (@vendorId IS NULL OR a.VendorId = @vendorId)",
            new { agreementId, vendorId }, cancellationToken: ct));

        return rows.Select(r => new AgreementChunk
        {
            Id = r.Id, AgreementId = r.AgreementId, ChunkIndex = r.ChunkIndex, Page = r.Page,
            Section = r.Section, Content = r.Content, Embedding = VectorMath.FromBytes(r.Embedding)
        }).ToList();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.VendorAgreement WHERE Id = @id", new { id }, cancellationToken: ct));
        return rows > 0;   // chunks and terms are removed by ON DELETE CASCADE
    }

    public async Task SaveTermsAsync(int agreementId, string termsJson, bool reviewed, string? reviewedBy, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE dbo.VendorAgreementTerms
                 SET TermsJson = @termsJson, ExtractedUtc = SYSUTCDATETIME(), Reviewed = @reviewed, ReviewedBy = @reviewedBy
               WHERE AgreementId = @agreementId;
              IF @@ROWCOUNT = 0
                INSERT INTO dbo.VendorAgreementTerms (AgreementId, TermsJson, ExtractedUtc, Reviewed, ReviewedBy)
                VALUES (@agreementId, @termsJson, SYSUTCDATETIME(), @reviewed, @reviewedBy);",
            new { agreementId, termsJson, reviewed, reviewedBy }, cancellationToken: ct));
    }

    public async Task<StoredTerms?> GetTermsAsync(int agreementId, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<TermsRow>(new CommandDefinition(
            "SELECT AgreementId, TermsJson, ExtractedUtc, Reviewed, ReviewedBy FROM dbo.VendorAgreementTerms WHERE AgreementId = @agreementId",
            new { agreementId }, cancellationToken: ct));
        return row is null ? null : new StoredTerms(row.AgreementId, row.TermsJson, row.ExtractedUtc, row.Reviewed, row.ReviewedBy);
    }

    // Row shapes for Dapper (DateOnly is not mapped by Dapper, so DateTime is used here).
    private sealed class AgreementRow
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public string Title { get; set; } = "";
        public string FileName { get; set; } = "";
        public string StoredPath { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public DateTime? EffectiveDate { get; set; }
        public int PageCount { get; set; }
        public int ChunkCount { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime UploadedUtc { get; set; }

        public AgreementDocument ToModel() => new()
        {
            Id = Id, VendorId = VendorId, Title = Title, FileName = FileName, StoredPath = StoredPath, Sha256 = Sha256,
            EffectiveDate = EffectiveDate is null ? null : DateOnly.FromDateTime(EffectiveDate.Value),
            PageCount = PageCount, ChunkCount = ChunkCount, UploadedBy = UploadedBy,
            UploadedUtc = DateTime.SpecifyKind(UploadedUtc, DateTimeKind.Utc)
        };
    }

    private sealed class ChunkRow
    {
        public int Id { get; set; }
        public int AgreementId { get; set; }
        public int ChunkIndex { get; set; }
        public int Page { get; set; }
        public string? Section { get; set; }
        public string Content { get; set; } = "";
        public byte[] Embedding { get; set; } = [];
    }

    private sealed class TermsRow
    {
        public int AgreementId { get; set; }
        public string TermsJson { get; set; } = "";
        public DateTime ExtractedUtc { get; set; }
        public bool Reviewed { get; set; }
        public string? ReviewedBy { get; set; }
    }
}
