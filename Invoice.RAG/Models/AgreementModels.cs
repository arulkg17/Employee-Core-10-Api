namespace Invoice.RAG.Models;

public sealed class AgreementDocument
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string StoredPath { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public DateOnly? EffectiveDate { get; set; }
    public int PageCount { get; set; }
    public int ChunkCount { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime UploadedUtc { get; set; }
}

public sealed class AgreementChunk
{
    public int Id { get; set; }
    public int AgreementId { get; set; }
    public int ChunkIndex { get; set; }
    public int Page { get; set; }
    public string? Section { get; set; }
    public string Content { get; set; } = "";
    public float[] Embedding { get; set; } = [];
}

public sealed record StoredTerms(int AgreementId, string TermsJson, DateTime ExtractedUtc, bool Reviewed, string? ReviewedBy);

/// <summary>Safe-to-return view of an agreement (no server file path).</summary>
public sealed record AgreementInfo(
    int Id, int VendorId, string Title, string FileName, DateOnly? EffectiveDate,
    int PageCount, int ChunkCount, string? UploadedBy, DateTime UploadedUtc)
{
    public static AgreementInfo From(AgreementDocument d) =>
        new(d.Id, d.VendorId, d.Title, d.FileName, d.EffectiveDate, d.PageCount, d.ChunkCount, d.UploadedBy, d.UploadedUtc);
}

public sealed class UploadAgreementRequest
{
    public IFormFile File { get; set; } = default!;
    public int VendorId { get; set; }
    public string? Title { get; set; }
    public DateOnly? EffectiveDate { get; set; }
}
