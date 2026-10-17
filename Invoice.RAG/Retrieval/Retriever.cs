using System.Text;
using Invoice.RAG.Configuration;
using Invoice.RAG.Models;
using Invoice.RAG.Ollama;
using Microsoft.Extensions.Options;

namespace Invoice.RAG.Retrieval;

public sealed record RetrievedChunk(AgreementChunk Chunk, string AgreementTitle, double Score);

public sealed class Retriever(IAgreementStore store, OllamaClient ollama, IOptions<RagOptions> options, ILogger<Retriever> logger)
{
    /// <summary>
    /// Finds the chunks most similar to each query, limited to one vendor and/or one agreement.
    /// Queries are embedded in a single call and the candidate set is loaded once.
    /// </summary>
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        IReadOnlyList<string> queries, int? vendorId, int? agreementId, int perQuery, double minScore, CancellationToken ct)
    {
        var candidates = await store.GetChunksAsync(agreementId, vendorId, ct);
        if (candidates.Count == 0) return [];

        var prefix = options.Value.Ollama.QueryPrefix;
        var queryVectors = (await ollama.EmbedAsync(queries.Select(q => prefix + q).ToList(), ct))
            .Select(VectorMath.Normalize).ToList();

        var dimension = queryVectors[0].Length;
        var usable = candidates.Where(c => c.Embedding.Length == dimension).ToList();
        if (usable.Count != candidates.Count)
            logger.LogWarning("{Count} chunk(s) were embedded with a different model and were skipped. Re-upload those agreements.",
                candidates.Count - usable.Count);

        var best = new Dictionary<int, double>();
        foreach (var qv in queryVectors)
        {
            var top = usable
                .Select(c => (c.Id, Score: VectorMath.Dot(c.Embedding, qv)))
                .Where(x => x.Score >= minScore)
                .OrderByDescending(x => x.Score)
                .Take(perQuery);

            foreach (var (id, score) in top)
                if (!best.TryGetValue(id, out var existing) || score > existing)
                    best[id] = score;
        }

        var byId = usable.ToDictionary(c => c.Id);
        var titles = new Dictionary<int, string>();
        foreach (var agId in best.Keys.Select(k => byId[k].AgreementId).Distinct())
            titles[agId] = (await store.GetAsync(agId, ct))?.Title ?? $"Agreement {agId}";

        return best
            .Select(kv => new RetrievedChunk(byId[kv.Key], titles[byId[kv.Key].AgreementId], kv.Value))
            .OrderByDescending(r => r.Score)
            .ToList();
    }
}

public static class ContextBuilder
{
    /// <summary>Numbers the best chunks as [1], [2]... (within a character budget) so the model can cite them.</summary>
    public static (string Text, List<SourceRef> Sources) Build(IReadOnlyList<RetrievedChunk> hits, int maxChars)
    {
        var picked = new List<RetrievedChunk>();
        var used = 0;
        foreach (var h in hits.OrderByDescending(h => h.Score))
        {
            if (used + h.Chunk.Content.Length > maxChars && picked.Count > 0) break;
            picked.Add(h);
            used += h.Chunk.Content.Length;
        }

        var ordered = picked.OrderBy(h => h.Chunk.AgreementId).ThenBy(h => h.Chunk.ChunkIndex).ToList();
        var sb = new StringBuilder();
        var sources = new List<SourceRef>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var h = ordered[i];
            var n = i + 1;
            sources.Add(new SourceRef(n, h.Chunk.AgreementId, h.AgreementTitle, h.Chunk.Page, h.Chunk.Section, Math.Round(h.Score, 3), Snippet(h.Chunk.Content)));

            var where = h.Chunk.Section is null ? "" : ", " + h.Chunk.Section;
            sb.AppendLine($"[{n}] ({h.AgreementTitle}, page {h.Chunk.Page}{where})");
            sb.AppendLine(h.Chunk.Content);
            sb.AppendLine();
        }

        return (sb.ToString(), sources);
    }

    private static string Snippet(string text) => text.Length <= 400 ? text : text[..400] + "...";
}
