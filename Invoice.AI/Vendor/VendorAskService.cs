using System.Diagnostics;
using System.Text.Json;
using Invoice.AI.Mcp;
using Invoice.AI.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Invoice.AI.Vendor;

/// <summary>
/// Semantic-Kernel-free implementation of the vendor Q&A orchestration.
/// It uses a local Ollama model (IOllamaClient) to produce a JSON plan of
/// tool calls, executes those tool calls through IMS.MCP.Server, then asks the
/// model to produce a final answer given the tool outputs.
/// This provides similar behaviour to the original Kernel-based flow without
/// introducing a Semantic Kernel dependency.
/// </summary>
public sealed class VendorAskService
{
    private static readonly HashSet<string> Hidden = new() { "ask_vendor_questions" };

    private readonly IOllamaClient _ollama;
    private readonly IOptions<ImsMcpOptions> _options;
    private readonly SkillPromptProvider _skills;
    private readonly ILogger<VendorAskService> _logger;
    private readonly IImsMcpSessionFactory _sessionFactory;

    public VendorAskService(IOllamaClient ollama, IOptions<ImsMcpOptions> options, SkillPromptProvider skills, ILogger<VendorAskService> logger, IImsMcpSessionFactory sessionFactory)
    {
        _ollama = ollama;
        _options = options;
        _skills = skills;
        _logger = logger;
        _sessionFactory = sessionFactory;
    }

    public async Task<VendorAskResult> AskAsync(VendorAskRequest request, string? authorization, CancellationToken ct)
    {
        var questions = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Question)) questions.Add(request.Question.Trim());
        questions.AddRange((request.Questions ?? []).Where(q => !string.IsNullOrWhiteSpace(q)).Select(q => q.Trim()));

        if (questions.Count == 0) throw new ArgumentException("Provide 'question' or 'questions'.");
        if (questions.Count > 8) throw new ArgumentException("Too many questions (max 8).");
        if (questions.Any(q => q.Length > 1000)) throw new ArgumentException("A question is too long (max 1000 characters).");

        var timer = Stopwatch.StartNew();

        await using var session = await _sessionFactory.CreateAsync(_options.Value.McpEndpoint, authorization, ct);
        var tools = (await session.ListToolsAsync(ct)).Where(t => !Hidden.Contains(t.Name)).ToList();

        var skillText = await _skills.BuildAsync(session, questions, ct);

        // Build system prompt (kept concise; the skill text is appended)
        var systemPrompt = $"You are the assistant of an Invoice Management System. Today is {DateTime.UtcNow:yyyy-MM-dd}.\n" +
                           "Answer the user's questions. Use the available tools when needed.\n" + skillText;

        var userMessage = questions.Count == 1
            ? questions[0]
            : "Answer each question:\n" + string.Join("\n", questions.Select((q, i) => $"{i + 1}. {q}"));

        // Ask the model for a plan: answer (optional) and list of tool calls to run
        var planSchema = new
        {
            type = "object",
            properties = new
            {
                answer = new { type = "string" },
                toolCalls = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            arguments = new { type = "object" }
                        },
                        required = new[] { "name" }
                    }
                }
            }
        };

        var toolCallInfos = new List<ToolCallInfo>();

        var maxIterations = _options.Value.MaxIterations;
        var maxToolCalls = _options.Value.MaxToolCalls;
        var totalToolCalls = 0;
        var currentUserMessage = userMessage;

        for (var iter = 0; iter < maxIterations; iter++)
        {
            string rawPlanJson = await _ollama.ChatJsonAsync(systemPrompt, currentUserMessage, planSchema, ct);
            using var planDoc = JsonDocument.Parse(rawPlanJson);
            var planRoot = planDoc.RootElement;

            // Collect tool calls
            var toolCalls = new List<(string Name, JsonElement Arguments)>();
            if (planRoot.TryGetProperty("toolCalls", out var callsElem) && callsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in callsElem.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    if (!item.TryGetProperty("name", out var nameElem)) continue;
                    var name = nameElem.GetString() ?? string.Empty;
                    var args = item.TryGetProperty("arguments", out var argsElem) ? argsElem : JsonDocument.Parse("{}").RootElement;
                    toolCalls.Add((name, args));
                }
            }

            if (toolCalls.Count == 0)
            {
                // If the model already returned a final answer in the plan, use it directly.
                if (planRoot.TryGetProperty("answer", out var planAnswer) && planAnswer.ValueKind == JsonValueKind.String && planAnswer.GetString() is string pa && pa.Length > 0)
                {
                    return new VendorAskResult
                    {
                        Answer = pa,
                        ToolCalls = toolCallInfos,
                        ToolsAvailable = tools.Select(t => t.Name).ToList(),
                        SkillLoaded = skillText.Length > 0,
                        ElapsedMs = timer.ElapsedMilliseconds
                    };
                }

                break; // nothing to do
            }

            // enforce cap
            var allowed = Math.Min(toolCalls.Count, Math.Max(0, maxToolCalls - totalToolCalls));
            if (allowed <= 0)
            {
                _logger.LogInformation("Max tool calls reached ({MaxToolCalls}); stopping iteration", maxToolCalls);
                break;
            }

            for (var i = 0; i < allowed; i++)
            {
                var (name, argsElem) = toolCalls[i];
                var sw = Stopwatch.StartNew();

                // Convert arguments JsonElement to Dictionary<string, object?>
                var dict = new Dictionary<string, object?>();
                if (argsElem.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in argsElem.EnumerateObject())
                    {
                        object? val;
                        var raw = prop.Value.GetRawText();
                        try
                        {
                            val = JsonSerializer.Deserialize<object>(raw);
                        }
                        catch
                        {
                            val = raw;
                        }
                        dict[prop.Name] = val;
                    }
                }

                string toolOutput;
                try
                {
                    toolOutput = await session.CallToolTextAsync(name, dict, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tool {Tool} failed", name);
                    toolOutput = $"<tool {name} failed: {ex.Message}>";
                }

                sw.Stop();
                toolCallInfos.Add(new ToolCallInfo(name, dict.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString()), sw.ElapsedMilliseconds));
                totalToolCalls++;
                // Attach tool output back into the user message so the model can use it in the next iteration
                currentUserMessage += $"\n\nTool output for {name}:\n{toolOutput}";
            }

            // continue to next iteration so the model can propose more tool calls or none
        }

        // Ask the model to produce the final answer given the tool outputs
        var finalSchema = new
        {
            type = "object",
            properties = new { answer = new { type = "string" } },
            required = new[] { "answer" }
        };

        string finalJson = await _ollama.ChatJsonAsync(systemPrompt, userMessage + "\n\nNow produce a concise answer to the user's questions.", finalSchema, ct);

        using var finalDoc = JsonDocument.Parse(finalJson);
        var finalRoot = finalDoc.RootElement;
        var answer = finalRoot.GetProperty("answer").GetString() ?? string.Empty;

        return new VendorAskResult
        {
            Answer = answer,
            ToolCalls = toolCallInfos,
            ToolsAvailable = tools.Select(t => t.Name).ToList(),
            SkillLoaded = skillText.Length > 0,
            ElapsedMs = timer.ElapsedMilliseconds
        };
    }
}
