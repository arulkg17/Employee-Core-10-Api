using Invoice.RAG.Models;

namespace Invoice.RAG.Retrieval;

public interface IAgreementStore
{
    Task<int> AddAgreementAsync(AgreementDocument doc, IReadOnlyList<AgreementChunk> chunks, CancellationToken ct);
    Task<AgreementDocument?> FindByHashAsync(int vendorId, string sha256, CancellationToken ct);
    Task<AgreementDocument?> GetAsync(int id, CancellationToken ct);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<AgreementDocument>> ListAsync(int? vendorId, CancellationToken ct);

    /// <summary>All chunks of the agreement / vendor (the candidate set for similarity search).</summary>
    Task<IReadOnlyList<AgreementChunk>> GetChunksAsync(int? agreementId, int? vendorId, CancellationToken ct);

    Task<bool> DeleteAsync(int id, CancellationToken ct);

    Task SaveTermsAsync(int agreementId, string termsJson, bool reviewed, string? reviewedBy, CancellationToken ct);
    Task<StoredTerms?> GetTermsAsync(int agreementId, CancellationToken ct);
}
