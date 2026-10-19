using System.Text.Json;
using System.Text.RegularExpressions;

namespace IMS.MCP.Server.Skills;

public sealed record SkillInfo(string Name, string Description, string Folder);

/// <summary>
/// Reads "skills": folders with a SKILL.md (name + description in the YAML header, rules in the body),
/// optional references/*.md and thresholds.json. Same layout as Anthropic's Agent Skills, so the folder can also be
/// used in Claude. Files are read on every call (they are small), so an edit shows up without a restart when
/// Ims:SkillsPath points at the source folder.
/// </summary>
public sealed partial class SkillCatalog(IConfiguration config)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string Root => Path.GetFullPath(config["Ims:SkillsPath"] ?? Path.Combine(AppContext.BaseDirectory, "Skills"));

    public IReadOnlyList<SkillInfo> List()
    {
        if (!Directory.Exists(Root)) return [];

        var skills = new List<SkillInfo>();
        foreach (var folder in Directory.GetDirectories(Root).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var file = Path.Combine(folder, "SKILL.md");
            if (!File.Exists(file)) continue;

            var (name, description, _) = Parse(File.ReadAllText(file));
            skills.Add(new SkillInfo(name ?? Path.GetFileName(folder), description ?? "", folder));
        }
        return skills;
    }

    public SkillInfo? Find(string name) =>
        List().FirstOrDefault(s => s.Name.Equals(name?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>The SKILL.md text without the YAML header.</summary>
    public string? ReadBody(string name)
    {
        var skill = Find(name);
        return skill is null ? null : Parse(File.ReadAllText(Path.Combine(skill.Folder, "SKILL.md"))).Body;
    }

    public IReadOnlyList<string> ReferenceFiles(string name)
    {
        var skill = Find(name);
        var dir = skill is null ? null : Path.Combine(skill.Folder, "references");
        return dir is not null && Directory.Exists(dir)
            ? Directory.GetFiles(dir).Select(Path.GetFileName).Where(f => f is not null).Select(f => f!).OrderBy(f => f).ToList()
            : [];
    }

    /// <summary>One reference file. Only a plain file name is accepted, so the caller cannot walk out of the folder.</summary>
    public string? ReadReference(string name, string fileName)
    {
        var skill = Find(name);
        if (skill is null || !SafeFileName().IsMatch(fileName ?? "")) return null;

        var path = Path.Combine(skill.Folder, "references", fileName!);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public P2pThresholds Thresholds(string skillName = "ims-purchase-to-pay")
    {
        var skill = Find(skillName);
        var path = skill is null ? null : Path.Combine(skill.Folder, "thresholds.json");
        if (path is null || !File.Exists(path)) return P2pThresholds.Defaults();

        try { return JsonSerializer.Deserialize<P2pThresholds>(File.ReadAllText(path), JsonOptions) ?? P2pThresholds.Defaults(); }
        catch (JsonException) { return P2pThresholds.Defaults(); }
    }

    private static (string? Name, string? Description, string Body) Parse(string text)
    {
        text = text.Replace("\r\n", "\n").TrimStart('\ufeff');
        if (!text.StartsWith("---\n", StringComparison.Ordinal)) return (null, null, text);

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0) return (null, null, text);

        string? name = null, description = null;
        foreach (var line in text[4..end].Split('\n'))
        {
            var i = line.IndexOf(':');
            if (i <= 0) continue;
            var key = line[..i].Trim();
            var value = line[(i + 1)..].Trim().Trim('"');
            if (key == "name") name = value;
            else if (key == "description") description = value;   // keep the description on ONE line in SKILL.md
        }

        return (name, description, text[(end + 4)..].TrimStart('\n'));
    }

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9_.-]*\.(md|json)$")]
    private static partial Regex SafeFileName();
}

public sealed class P2pThresholds
{
    public List<ApprovalLimit> ApprovalLimits { get; set; } = [];
    public MatchTolerance Match { get; set; } = new();
    public decimal OverReceiptTolerancePercent { get; set; } = 5m;
    public int DefaultPaymentTermsDays { get; set; } = 30;
    public string PaymentRunDay { get; set; } = "Friday";
    public decimal SecondApprovalAbove { get; set; } = 25000m;

    public static P2pThresholds Defaults() => new()
    {
        ApprovalLimits =
        [
            new ApprovalLimit { UpTo = 5000m, Approver = "Department manager" },
            new ApprovalLimit { UpTo = 25000m, Approver = "Finance manager" },
            new ApprovalLimit { UpTo = null, Approver = "CFO" }
        ]
    };
}

public sealed class ApprovalLimit
{
    public decimal? UpTo { get; set; }
    public string Approver { get; set; } = "";
}

public sealed class MatchTolerance
{
    public decimal QuantityTolerancePercent { get; set; } = 5m;
    public decimal PriceTolerancePercent { get; set; } = 2m;
    public decimal AmountToleranceAbsolute { get; set; } = 50m;
}
