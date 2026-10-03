namespace Invoice.AI.Services;

/// <summary>Maps a spelling the LLM returned (e.g. "Vegetables") to the real database value ("Vegitables").</summary>
public static class FuzzyMatcher
{
    public static string? BestMatch(string? input, IEnumerable<string> candidates, int maxDistance = 2)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var needle = Normalize(input);
        string? best = null;
        var bestDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;

            var distance = Levenshtein(needle, Normalize(candidate));
            if (distance == 0) return candidate.Trim();

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate.Trim();
            }
        }

        // short words are allowed fewer mistakes than long words
        var limit = Math.Min(maxDistance, Math.Max(1, needle.Length / 3));
        return bestDistance <= limit ? best : null;
    }

    private static string Normalize(string s) => s.Trim().ToLowerInvariant();

    private static int Levenshtein(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }
}
