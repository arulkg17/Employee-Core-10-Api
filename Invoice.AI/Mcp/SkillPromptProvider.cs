using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Invoice.AI.Mcp;

/// <summary>
/// Adds the company skill to the system prompt when a question needs it. The rules are NOT copied into Invoice.AI:
/// they are read from IMS.MCP.Server (tool get_skill), so there is one source of truth for Copilot, Inspector and Invoice.AI.
/// A local 7B model has a small context, so the core rules (about 65 lines) are added only for purchase-to-pay questions;
/// details are fetched on demand with get_skill_reference or search_p2p_policy.
/// </summary>
public sealed partial class SkillPromptProvider(ILogger<SkillPromptProvider> logger)
{
    private const string SkillName = "ims-purchase-to-pay";

    private static readonly object Gate = new();
    private static (string Text, DateTime LoadedUtc)? _cache;

    /// <summary>Returns text to append to the system prompt, or "" when the questions are not about purchase-to-pay.</summary>
    public async Task<string> BuildAsync(IImsMcpSession session, IEnumerable<string> questions, CancellationToken ct)
    {
        if (!questions.Any(q => Triggers().IsMatch(q))) return "";

        string? body;
        lock (Gate)
            body = _cache is { } c && DateTime.UtcNow - c.LoadedUtc < TimeSpan.FromMinutes(10) ? c.Text : null;

        if (body is null)
        {
            try
            {
                body = await session.CallToolTextAsync("get_skill",
                    new Dictionary<string, object?> { ["name"] = SkillName }, ct);
                lock (Gate) _cache = (body, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                // The skill is a help, not a requirement: keep answering with the tools alone.
                logger.LogWarning(ex, "Could not load skill {Skill} from IMS.MCP.Server", SkillName);
                return "";
            }
        }

        return $"""


            Company skill "{SkillName}" (follow it when the question is about purchase orders, goods receipts, vendor invoices or vendor payments):
            {body}
            """;
    }

    [GeneratedRegex(@"\b(po|pos|purchase orders?|receipts?|grn|goods received|received|invoices?|payments?|pay|paid|due|overdue|approv\w*|match\w*|tolerance|hold|discount|late (fee|interest|penalty))\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex Triggers();
}
