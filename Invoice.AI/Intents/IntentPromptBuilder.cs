using System.Text;
using Invoice.AI.Abstractions;
using Invoice.AI.Models;

namespace Invoice.AI.Intents;

/// <summary>
/// Builds the system prompt and the JSON schema from the registered handlers.
/// Add a handler -> the LLM automatically learns the new intent.
/// Speed notes: the prompt is identical for Single and Multi except the LAST rule line
/// (so Ollama can reuse its cache), and the model only writes the fields it needs.
/// </summary>
public sealed class IntentPromptBuilder
{
    // Same examples for both modes. Fields that are not needed are simply left out.
    private static readonly (string Question, string Json)[] Examples =
    {
        ("How many active items are in Rice?",
         """{"intents":[{"intent":"CategoryItemCount","phrase":"How many active items are in Rice","categoryName":"Rice","activeFilter":"active"}]}"""),

        ("How many customers are in Pune?",
         """{"intents":[{"intent":"CustomerCountByCity","phrase":"How many customers are in Pune","city":"Pune"}]}"""),

        ("What is the weather today?",
         """{"intents":[{"intent":"Unknown","phrase":"What is the weather today"}]}"""),

        ("How many active customers and inactive vendors are there?",
         """{"intents":[{"intent":"CustomerCount","phrase":"How many active customers","activeFilter":"active"},{"intent":"VendorCount","phrase":"inactive vendors","activeFilter":"inactive"}]}"""),

        ("How many items are in Oil, how many users and how many categories?",
         """{"intents":[{"intent":"CategoryItemCount","phrase":"How many items are in Oil","categoryName":"Oil"},{"intent":"UserCount","phrase":"how many users"},{"intent":"CategoryCount","phrase":"how many categories"}]}""")
    };

    public string BuildSystemPrompt(
        AskMode mode,
        IEnumerable<IIntentHandler> handlers,
        IEnumerable<string> categories,
        IEnumerable<string> cities)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an intent-extraction engine for an Invoice Management System.");
        sb.AppendLine("You NEVER answer the question yourself. You only convert it into JSON.");
        sb.AppendLine();

        sb.AppendLine("SUPPORTED INTENTS:");
        foreach (var h in handlers)
            sb.AppendLine($"- {h.IntentName}: {h.Description}");
        sb.AppendLine($"- {IntentNames.Unknown}: the question is not about any intent above.");
        sb.AppendLine();

        var categoryList = string.Join(", ", categories);
        var cityList = string.Join(", ", cities);
        sb.AppendLine("KNOWN CATEGORIES: " + (categoryList.Length == 0 ? "(none)" : categoryList));
        sb.AppendLine("KNOWN CITIES: " + (cityList.Length == 0 ? "(none)" : cityList));
        sb.AppendLine();

        sb.AppendLine("FIELDS OF EVERY INTENT OBJECT:");
        sb.AppendLine("- intent: one of the supported intents. (always)");
        sb.AppendLine("- phrase: the exact words from the user's question that belong to this intent. (always)");
        sb.AppendLine("- categoryName: the closest KNOWN CATEGORY (fix spelling). Leave it out if not needed.");
        sb.AppendLine("- city: the closest KNOWN CITY (fix spelling). Leave it out if not needed.");
        sb.AppendLine("- activeFilter: \"active\" ONLY if the exact word \"active\" is in the phrase,");
        sb.AppendLine("  \"inactive\" ONLY if the exact word \"inactive\" is in the phrase.");
        sb.AppendLine("  Leave it out if neither word is in the phrase.");
        sb.AppendLine();

        sb.AppendLine("Output ONLY the JSON object. No explanation, no markdown.");
        sb.AppendLine();

        sb.AppendLine("EXAMPLES:");
        foreach (var (q, json) in Examples)
        {
            sb.AppendLine("Q: " + q);
            sb.AppendLine(json);
            sb.AppendLine();
        }

        // Keep this mode-specific line LAST: everything above is identical for both modes,
        // so Ollama's prompt cache is reused when you switch between /ask and /ask-multi.
        sb.AppendLine(mode == AskMode.Single
            ? "RULE FOR THIS REQUEST: return EXACTLY ONE object inside \"intents\"."
            : "RULE FOR THIS REQUEST: the user may ask several questions. Return ONE object per question, in the order asked. Never merge or skip a question.");

        return sb.ToString();
    }

    /// <summary>JSON schema sent in Ollama's "format" field. Forces the exact output shape.</summary>
    public object BuildSchema(AskMode mode, IEnumerable<string> intentNames)
    {
        var names = intentNames.Append(IntentNames.Unknown).Distinct().ToArray();

        var itemSchema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["intent"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = names },
                ["phrase"] = new Dictionary<string, object> { ["type"] = "string" },
                ["categoryName"] = new Dictionary<string, object> { ["type"] = "string" },
                ["city"] = new Dictionary<string, object> { ["type"] = "string" },
                ["activeFilter"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "active", "inactive" }
                }
            },
            // Only these two are mandatory. The others are optional, so the model writes fewer tokens.
            ["required"] = new[] { "intent", "phrase" }
        };

        // Note: no maxItems on purpose. If the user asks two questions on the single endpoint
        // we want to notice it and tell them (see AIOrchestrator).
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["intents"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["minItems"] = 1,
                    ["items"] = itemSchema
                }
            },
            ["required"] = new[] { "intents" }
        };
    }
}