using System.Text.RegularExpressions;

namespace Invoice.AI.Services;

/// <summary>
/// Small models often confuse "active" and "inactive". The LLM is only a HINT here -
/// C# decides the final true/false/null by looking at the real words of the question.
/// </summary>
public static class ActiveFilterResolver
{
    private static readonly Regex InactiveWord = new(
        @"\b(inactive|in-active|in active|disabled|deactivated|not active)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ActiveWord = new(
        @"\b(active|enabled)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <returns>true = active only, false = inactive only, null = no filter.</returns>
    public static bool? Resolve(string question, string? phrase, string? llmFilter)
    {
        // 1. The user never said active/inactive anywhere -> no filter, whatever the LLM said.
        if (!InactiveWord.IsMatch(question) && !ActiveWord.IsMatch(question))
            return null;

        // 2. The words of THIS intent decide (important for multi-intent questions).
        if (!string.IsNullOrWhiteSpace(phrase))
        {
            if (InactiveWord.IsMatch(phrase)) return false;
            if (ActiveWord.IsMatch(phrase)) return true;
        }

        // 3. The word exists in the question but not in the phrase -> fall back to the LLM hint.
        return llmFilter?.Trim().ToLowerInvariant() switch
        {
            "active" => true,
            "inactive" => false,
            _ => null
        };
    }
}
