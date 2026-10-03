using System.Text;
using Invoice.AI.Abstractions;
using Invoice.AI.Models;

namespace Invoice.AI.Intents;

/// <summary>
/// Builds the system prompt and the JSON schema from the registered handlers.
/// Add a handler -> the LLM automatically learns the new intent.
/// </summary>
public sealed class IntentPromptBuilder
{
    private static readonly (string Question, string Json)[] SingleExamples =
    {
        ("How many active items are in Rice?",
         """{"intents":[{"intent":"CategoryItemCount","phrase":"How many active items are in Rice","categoryName":"Rice","city":"","activeFilter":"active"}]}"""),

        ("How many inactive items are in Snacks?",
         """{"intents":[{"intent":"CategoryItemCount","phrase":"How many inactive items are in Snacks","categoryName":"Snacks","city":"","activeFilter":"inactive"}]}"""),

        ("How many items are there in total?",
         """{"intents":[{"intent":"ItemCount","phrase":"How many items are there in total","categoryName":"","city":"","activeFilter":"any"}]}"""),

        ("How many customers are in Pune?",
         """{"intents":[{"intent":"CustomerCountByCity","phrase":"How many customers are in Pune","categoryName":"","city":"Pune","activeFilter":"any"}]}"""),

        ("Show customer count for every city",
         """{"intents":[{"intent":"CustomerCountAllCities","phrase":"Show customer count for every city","categoryName":"","city":"","activeFilter":"any"}]}"""),

        ("What is the weather today?",
         """{"intents":[{"intent":"Unknown","phrase":"What is the weather today","categoryName":"","city":"","activeFilter":"any"}]}""")
    };

    private static readonly (string Question, string Json)[] MultiExamples =
    {
        ("How many active customers and inactive vendors are there?",
         """{"intents":[{"intent":"CustomerCount","phrase":"How many active customers","categoryName":"","city":"","activeFilter":"active"},{"intent":"VendorCount","phrase":"inactive vendors","categoryName":"","city":"","activeFilter":"inactive"}]}"""),

        ("How many items are in Oil, how many users and how many categories?",
         """{"intents":[{"intent":"CategoryItemCount","phrase":"How many items are in Oil","categoryName":"Oil","city":"","activeFilter":"any"},{"intent":"UserCount","phrase":"how many users","categoryName":"","city":"","activeFilter":"any"},{"intent":"CategoryCount","phrase":"how many categories","categoryName":"","city":"","activeFilter":"any"}]}""")
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
        sb.AppendLine("- intent: one of the supported intents.");
        sb.AppendLine("- phrase: the exact words from the user's question that belong to this intent.");
        sb.AppendLine("- categoryName: the closest KNOWN CATEGORY (fix spelling and plurals), or \"\" if not needed.");
        sb.AppendLine("- city: the closest KNOWN CITY (fix spelling), or \"\" if not needed.");
        sb.AppendLine("- activeFilter: \"active\" ONLY if the exact word \"active\" is in the phrase,");
        sb.AppendLine("  \"inactive\" ONLY if the exact word \"inactive\" is in the phrase, otherwise \"any\".");
        sb.AppendLine("  \"active\" and \"inactive\" are DIFFERENT words - check the letters.");
        sb.AppendLine();

        sb.AppendLine("RULES:");
        if (mode == AskMode.Single)
        {
            sb.AppendLine("- Return EXACTLY ONE object inside \"intents\".");
        }
        else
        {
            sb.AppendLine("- The user may ask several questions in one sentence.");
            sb.AppendLine("- Return ONE object per question, in the order they were asked.");
            sb.AppendLine("- Never merge two questions into one object and never skip a question.");
        }
        sb.AppendLine("- Output ONLY the JSON object. No explanation, no markdown.");
        sb.AppendLine();

        sb.AppendLine("EXAMPLES:");
        foreach (var (q, json) in SingleExamples)
        {
            sb.AppendLine("Q: " + q);
            sb.AppendLine(json);
            sb.AppendLine();
        }

        if (mode == AskMode.Multi)
        {
            foreach (var (q, json) in MultiExamples)
            {
                sb.AppendLine("Q: " + q);
                sb.AppendLine(json);
                sb.AppendLine();
            }
        }

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
                    ["enum"] = new[] { "any", "active", "inactive" }
                }
            },
            ["required"] = new[] { "intent", "phrase", "categoryName", "city", "activeFilter" }
        };

        // Note: Single mode does NOT set maxItems on purpose. If the user asks two questions
        // on the single endpoint we want to notice it and tell them (see AIOrchestrator).
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
