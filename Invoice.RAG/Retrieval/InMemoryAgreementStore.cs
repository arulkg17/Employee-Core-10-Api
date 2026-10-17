using Invoice.RAG.Models;

namespace Invoice.RAG.Retrieval;

/// <summary>Demo store: nothing to install, but everything is lost when the app restarts.</summary>
public sealed class InMemoryAgreementStore : IAgreementStore
{
    private readonly object _lock = new();
    private readonly List<AgreementDocument> _docs = [];
    private readonly List<AgreementChunk> _chunks = [];
    private readonly Dictionary<int, StoredTerms> _terms = [];
    private int _nextDocId = 1;
    private int _nextChunkId = 1;

    public Task<int> AddAgreementAsync(AgreementDocument doc, IReadOnlyList<AgreementChunk> chunks, CancellationToken ct)
    {
        lock (_lock)
        {
            doc.Id = _nextDocId++;
            _docs.Add(doc);
            foreach (var c in chunks)
            {
                c.Id = _nextChunkId++;
                c.AgreementId = doc.Id;
                _chunks.Add(c);
            }
            return Task.FromResult(doc.Id);
        }
    }

    public Task<AgreementDocument?> FindByHashAsync(int vendorId, string sha256, CancellationToken ct)
    {
        lock (_lock)
            return Task.FromResult(_docs.FirstOrDefault(d => d.VendorId == vendorId && d.Sha256 == sha256));
    }

    public Task<AgreementDocument?> GetAsync(int id, CancellationToken ct)
    {
        lock (_lock)
            return Task.FromResult(_docs.FirstOrDefault(d => d.Id == id));
    }

    public Task<IReadOnlyList<AgreementDocument>> ListAsync(int? vendorId, CancellationToken ct)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<AgreementDocument>>(
                _docs.Where(d => vendorId is null || d.VendorId == vendorId)
                     .OrderByDescending(d => d.UploadedUtc).ThenByDescending(d => d.Id).ToList());
    }

    public Task<IReadOnlyList<AgreementChunk>> GetChunksAsync(int? agreementId, int? vendorId, CancellationToken ct)
    {
        lock (_lock)
        {
            var allowed = _docs
                .Where(d => (agreementId is null || d.Id == agreementId) && (vendorId is null || d.VendorId == vendorId))
                .Select(d => d.Id).ToHashSet();

            return Task.FromResult<IReadOnlyList<AgreementChunk>>(
                _chunks.Where(c => allowed.Contains(c.AgreementId)).ToList());
        }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        lock (_lock)
        {
            var removed = _docs.RemoveAll(d => d.Id == id) > 0;
            _chunks.RemoveAll(c => c.AgreementId == id);
            _terms.Remove(id);
            return Task.FromResult(removed);
        }
    }

    public Task SaveTermsAsync(int agreementId, string termsJson, bool reviewed, string? reviewedBy, CancellationToken ct)
    {
        lock (_lock)
            _terms[agreementId] = new StoredTerms(agreementId, termsJson, DateTime.UtcNow, reviewed, reviewedBy);
        return Task.CompletedTask;
    }

    public Task<StoredTerms?> GetTermsAsync(int agreementId, CancellationToken ct)
    {
        lock (_lock)
            return Task.FromResult(_terms.GetValueOrDefault(agreementId));
    }
}
