namespace Invoice.AI.Intents.Handlers;

/// <summary>Builds the final sentences in C# - no second LLM call, so answers are fast and always correct.</summary>
internal static class AnswerText
{
    public static string Qualifier(bool? isActive) => isActive switch
    {
        true => "active ",
        false => "inactive ",
        _ => string.Empty
    };

    /// <summary>"There are 4 active customers in Chennai."</summary>
    public static string Count(int n, bool? isActive, string singular, string plural, string? suffix = null)
    {
        var noun = n == 1 ? singular : plural;
        var verb = n == 1 ? "is" : "are";
        var tail = string.IsNullOrWhiteSpace(suffix) ? string.Empty : " " + suffix;
        return $"There {verb} {n} {Qualifier(isActive)}{noun}{tail}.";
    }
}
