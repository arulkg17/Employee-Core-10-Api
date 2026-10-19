using System.Text.Json;
using Invoice.AI.Abstractions;
using Invoice.AI.Mcp;
using Invoice.AI.Vendor;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Invoice.AI.Tests;

public class VendorAskServiceTests
{
    private sealed class SequencedOllama : IOllamaClient
    {
        private readonly Queue<string> _responses;
        public SequencedOllama(IEnumerable<string> responses) => _responses = new Queue<string>(responses);

        public Task<string> ChatJsonAsync(string systemPrompt, string userMessage, object jsonSchema, CancellationToken ct = default)
        {
            if (_responses.Count == 0) return Task.FromResult("{\"answer\":\"\",\"toolCalls\":[]}");
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class FakeSession : IImsMcpSession
    {
        private readonly Dictionary<string, string> _toolOutputs;

        public FakeSession(Dictionary<string, string>? toolOutputs = null)
        {
            _toolOutputs = toolOutputs ?? new Dictionary<string, string>();
        }

        public Task<IList<ModelContextProtocol.Client.McpClientTool>> ListToolsAsync(CancellationToken ct)
            => Task.FromResult((IList<ModelContextProtocol.Client.McpClientTool>)new List<ModelContextProtocol.Client.McpClientTool>());

        public Task<string> CallToolTextAsync(string name, IReadOnlyDictionary<string, object?> arguments, CancellationToken ct)
        {
            if (_toolOutputs.TryGetValue(name, out var v)) return Task.FromResult(v);
            // echo behavior for test: return first arg value if present
            var first = arguments.Values.FirstOrDefault()?.ToString() ?? "";
            return Task.FromResult(first);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeFactory : IImsMcpSessionFactory
    {
        private readonly IImsMcpSession _session;
        public FakeFactory(IImsMcpSession session) => _session = session;
        public Task<IImsMcpSession> CreateAsync(string endpoint, string? authorization, CancellationToken ct) => Task.FromResult(_session);
    }

    [Fact]
    public async Task IterativeToolCalls_ProducesFinalAnswer()
    {
        var plan1 = JsonSerializer.Serialize(new { answer = "", toolCalls = new[] { new { name = "echo", arguments = new { text = "Hello" } } } });
        var plan2 = JsonSerializer.Serialize(new { answer = "Final Answer", toolCalls = Array.Empty<object>() });

        var ollama = new SequencedOllama(new[] { plan1, plan2 });
        var session = new FakeSession();
        var factory = new FakeFactory(session);

        var options = Options.Create(new ImsMcpOptions { McpEndpoint = "http://x/", MaxIterations = 3, MaxToolCalls = 10 });
        var skills = new Invoice.AI.Mcp.SkillPromptProvider(NullLogger<Invoice.AI.Mcp.SkillPromptProvider>.Instance);
        var svc = new VendorAskService(ollama, options, skills, NullLogger<VendorAskService>.Instance, factory);

        var req = new VendorAskRequest { Question = "What about vendor X?" };
        var res = await svc.AskAsync(req, null, CancellationToken.None);

        Assert.Equal("Final Answer", res.Answer);
        Assert.Single(res.ToolCalls);
        Assert.Equal("echo", res.ToolCalls[0].Tool);
    }

    [Fact]
    public async Task ToolCallCap_IsEnforced()
    {
        var planMany = JsonSerializer.Serialize(new
        {
            answer = "",
            toolCalls = new[]
            {
                new { name = "a", arguments = new { x = 1 } },
                new { name = "b", arguments = new { x = 2 } },
                new { name = "c", arguments = new { x = 3 } }
            }
        });
        var planFinal = JsonSerializer.Serialize(new { answer = "After cap", toolCalls = Array.Empty<object>() });

        var ollama = new SequencedOllama(new[] { planMany, planFinal });
        var session = new FakeSession();
        var factory = new FakeFactory(session);

        var options = Options.Create(new ImsMcpOptions { McpEndpoint = "http://x/", MaxIterations = 5, MaxToolCalls = 1 });
        var skills = new Invoice.AI.Mcp.SkillPromptProvider(NullLogger<Invoice.AI.Mcp.SkillPromptProvider>.Instance);
        var svc = new VendorAskService(ollama, options, skills, NullLogger<VendorAskService>.Instance, factory);

        var req = new VendorAskRequest { Question = "Run many tools" };
        var res = await svc.AskAsync(req, null, CancellationToken.None);

        Assert.Equal("After cap", res.Answer);
        Assert.Single(res.ToolCalls);
    }
}
