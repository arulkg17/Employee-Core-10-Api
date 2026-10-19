using System.ComponentModel;
using System.Text;
using System.Text.Json;
using IMS.MCP.Server.Clients;
using IMS.MCP.Server.Skills;
using ModelContextProtocol.Server;

namespace IMS.MCP.Server.Tools;

/// <summary>
/// Gives any MCP client (Copilot agent mode, MCP Inspector, Invoice.AI) the company's skills: the rules for a domain,
/// loaded in steps. list_skills -> get_skill (the core rules) -> get_skill_reference (details), plus semantic search
/// over the rules through Invoice.RAG for clients with a small context.
/// </summary>
[McpServerToolType]
public sealed class SkillTools(SkillCatalog catalog, ImsApi api)
{
    private const int MaxChars = 12000;

    [McpServerTool(Name = "list_skills"),
     Description("List the company skills (rule sets) available, with a one-line description of when each applies. " +
                 "Call it when a question is about purchase orders, goods receipts, vendor invoices or vendor payments.")]
    public string ListSkills()
        => JsonSerializer.Serialize(catalog.List().Select(s => new
        {
            name = s.Name,
            description = s.Description,
            references = catalog.ReferenceFiles(s.Name)
        }), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [McpServerTool(Name = "get_skill"),
     Description("Get the core rules of a skill, for example 'ims-purchase-to-pay' (purchase orders, goods receipts, vendor payments). " +
                 "Read it before answering rule questions. It also says which tools to use and lists the reference files.")]
    public string GetSkill([Description("Skill name from list_skills.")] string name)
    {
        var body = catalog.ReadBody(name);
        if (body is null) return $"Skill '{name}' was not found. Call list_skills.";

        var refs = catalog.ReferenceFiles(name);
        var text = body + (refs.Count == 0 ? "" : "\n\nReference files (get_skill_reference): " + string.Join(", ", refs));
        return Cap(text);
    }

    [McpServerTool(Name = "get_skill_reference"),
     Description("Get one detailed reference file of a skill, for example 'vendor-payments.md' of 'ims-purchase-to-pay'. " +
                 "Use it when the core rules are not enough.")]
    public string GetSkillReference(
        [Description("Skill name from list_skills.")] string name,
        [Description("File name, for example vendor-payments.md.")] string file)
    {
        var text = catalog.ReadReference(name, file);
        return text is null
            ? $"Reference '{file}' of skill '{name}' was not found. Available: {string.Join(", ", catalog.ReferenceFiles(name))}"
            : Cap(text);
    }

    [McpServerTool(Name = "search_p2p_policy"),
     Description("Search the purchase-to-pay rules by meaning and get only the matching passages (cheaper than reading whole files). " +
                 "Use it for a specific rule question, for example 'over-receipt tolerance' or 'can a PO be cancelled after receipt'.")]
    public Task<string> SearchPolicy(
        [Description("What rule you are looking for, in plain words.")] string query,
        [Description("Optional. Number of passages, 1 to 8. Default 4.")] int? topK = null,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            var text = query?.Trim() ?? "";
            if (text.Length == 0) return new { error = "'query' is required." };

            var url = api.Path("PolicySearch", "api/v1/policies/search")
                      + "?query=" + Uri.EscapeDataString(text) + "&topK=" + Math.Clamp(topK ?? 4, 1, 8);
            var data = await api.GetAsync(ImsApi.Rag, url, allowNotFound: true, ct);

            var passages = new List<object>();
            if (data is not null)
                foreach (var hit in Js.Rows(data.Value))
                    passages.Add(new
                    {
                        skill = Js.Str(hit, "skill"),
                        file = Js.Str(hit, "file"),
                        section = Js.Str(hit, "section"),
                        score = Js.Dbl(hit, "score"),
                        text = Js.Str(hit, "text")
                    });

            return passages.Count == 0
                ? new { passages, note = "No matching rule found. Use get_skill to read the core rules." }
                : new { passages, note = (string?)null };
        });

    private static string Cap(string text) => text.Length <= MaxChars ? text : text[..MaxChars] + "\n...[cut]";
}
