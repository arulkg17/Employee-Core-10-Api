using Invoice.AI.Tools;
using Xunit;

namespace Invoice.AI.Tests
{
    public class McpToolTests
    {
        [Fact]
        public void GetGreeting_ReturnsExpectedGreeting()
        {
            var tool = new McpTool();
            var greeting = tool.GetGreeting();
            Assert.Equal("Hello from MCP", greeting);
        }
    }
}
