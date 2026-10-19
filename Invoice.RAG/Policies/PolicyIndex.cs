using System.Text;
using System.Text.RegularExpressions;
using Invoice.RAG.Configuration;
using Invoice.RAG.Ollama;
using Invoice.RAG.Retrieval;
using Microsoft.Extensions.Options;

namespace Invoice.RAG.Policies;

public sealed record PolicyHit(string Skill, string File, string? Section, double Score, string Text);

/// <summary>
/// Semantic search over the company rule files (skills/*/SKILL.md and references/*.md). The rules are small, so they are kept in
/// memory, embedded once with the same Ollama embedding model as the agreements, and reloaded when a file changes.
/// No database table is needed and nothing is mixed into the vendor agreement data.
/// </summary>
public sealed partial class PolicyIndex(OllamaClient ollama, IOptions<RagOptions> options, IConfiguration config, ILogger<PolicyIndex> logger)
{
    private sealed record Chunk(string Skill, string File, string? Section, string Text, float[] Vector);

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static List<Chunk>? _chunks;
    private static string? _stamp;

    private string Root => Path.GetFullPath(config["Rag:PoliciesPath"] ?? Path.Combine(AppContext.BaseDirectory, "Skills"));

    public async Task<IReadOnlyList<PolicyHit>> SearchAsync(string query, int topK, CancellationToken ct)
    {
        var chunks = await EnsureAsync(ct);
        if (chunks.Count == 0) return [];

        var o = options.Value;
        var qv = VectorMath.Normalize((await ollama.EmbedAsync([o.Ollama.QueryPrefix + query], ct))[0]);

        return chunks
            .Where(c => c.Vector.Length == qv.Length)
            .Select(c => (Chunk: c, Score: VectorMath.Dot(c.Vector, qv)))
            .Where(x => x.Score >= o.MinScore)
            .OrderByDescending(x => x.Score)
            .Take(Math.Clamp(topK, 1, 8))
            .Select(x => new PolicyHit(x.Chunk.Skill, x.Chunk.File, x.Chunk.Section, Math.Round(x.Score, 3), x.Chunk.Text))
            .ToList();
    }

    private async Task<List<Chunk>> EnsureAsync(CancellationToken ct)
    {
        var files = Directory.Exists(Root)
            ? Directory.GetFiles(Root, "*.md", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal).ToList()
            : [];
        var stamp = string.Join("|", files.Select(f => f + File.GetLastWriteTimeUtc(f).Ticks)) + "|" + options.Value.Ollama.EmbeddingModel;

        if (_chunks is not null && _stamp == stamp) return _chunks;

        await Gate.WaitAsync(ct);
        try
        {
            if (_chunks is not null && _stamp == stamp) return _chunks;

            var raw = new List<(string Skill, string File, string? Section, string Text)>();
            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(Root, file).Replace('\\', '/');
                var parts = relative.Split('/');
                var skill = parts.Length > 1 ? parts[0] : "general";
                var name = string.Join("/", parts.Skip(1));
                foreach (var (section, text) in Split(File.ReadAllText(file)))
                    raw.Add((skill, name.Length == 0 ? relative : name, section, text));
            }

            var prefix = options.Value.Ollama.DocumentPrefix;
            var inputs = raw.Select(r => prefix + (r.Section is null ? "" : r.Section + "\n") + r.Text).ToList();

            var vectors = new List<float[]>(inputs.Count);
            for (var i = 0; i < inputs.Count; i += 16)
                vectors.AddRange(await ollama.EmbedAsync(inputs.Skip(i).Take(16).ToList(), ct));

            _chunks = raw.Select((r, i) => new Chunk(r.Skill, r.File, r.Section, r.Text, VectorMath.Normalize(vectors[i]))).ToList();
            _stamp = stamp;
            logger.LogInformation("Policy index built: {Count} passages from {Files} files", _chunks.Count, files.Count);
            return _chunks;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Splits a markdown file at headings; a long section is split further at blank lines (about 900 characters).</summary>
    private static IEnumerable<(string? Section, string Text)> Split(string markdown)
    {
        var text = FrontMatter().Replace(markdown.Replace("\r\n", "\n"), "");
        var heading = (string?)null;
        var body = new StringBuilder();
        var result = new List<(string?, string)>();

        void Flush()
        {
            foreach (var piece in Pack(body.ToString())) result.Add((heading, piece));
            body.Clear();
        }

        foreach (var line in text.Split('\n'))
        {
            var m = Heading().Match(line);
            if (m.Success)
            {
                Flush();
                heading = m.Groups[1].Value.Trim();
            }
            else
            {
                body.AppendLine(line);
            }
        }
        Flush();
        return result;
    }

    private static IEnumerable<string> Pack(string section)
    {
        var current = new StringBuilder();
        foreach (var paragraph in section.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length > 0 && current.Length + paragraph.Length > 900)
            {
                yield return current.ToString().Trim();
                current.Clear();
            }
            current.AppendLine(paragraph).AppendLine();
        }
        var last = current.ToString().Trim();
        if (last.Length >= 40) yield return last;
    }

    [GeneratedRegex(@"^#{1,4}\s+(.+)$")]
    private static partial Regex Heading();

    [GeneratedRegex(@"\A---\n.*?\n---\n", RegexOptions.Singleline)]
    private static partial Regex FrontMatter();
}
