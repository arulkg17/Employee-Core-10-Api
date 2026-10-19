using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using IMS.MCP.Server.Skills;
using Xunit;

namespace IMS.MCP.Server.Test;

public class SkillCatalogTests
{
    [Fact]
    public void SkillCatalog_ReadsSkillAndReferences()
    {
        var root = Path.Combine(Path.GetTempPath(), "skills_test_" + System.Guid.NewGuid().ToString("N"));
        try
        {
            var skillDir = Path.Combine(root, "skill1");
            Directory.CreateDirectory(Path.Combine(skillDir, "references"));

            File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "---\nname: \"Skill1\"\ndescription: \"Desc\"\n---\nThis is the body.");
            File.WriteAllText(Path.Combine(skillDir, "references", "ref1.md"), "Ref Content");
            File.WriteAllText(Path.Combine(skillDir, "thresholds.json"), "{ \"OverReceiptTolerancePercent\": 3.5, \"DefaultPaymentTermsDays\": 45 }");

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Ims:SkillsPath"] = root }).Build();
            var catalog = new SkillCatalog(config);

            var list = catalog.List();
            Assert.Contains(list, s => s.Name == "Skill1");

            var body = catalog.ReadBody("Skill1");
            Assert.Equal("This is the body.", body?.Trim());

            var refs = catalog.ReferenceFiles("Skill1");
            Assert.Contains("ref1.md", refs);

            var refText = catalog.ReadReference("Skill1", "ref1.md");
            Assert.Equal("Ref Content", refText);

            var thresholds = catalog.Thresholds("Skill1");
            Assert.Equal(3.5m, thresholds.OverReceiptTolerancePercent);
            Assert.Equal(45, thresholds.DefaultPaymentTermsDays);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
