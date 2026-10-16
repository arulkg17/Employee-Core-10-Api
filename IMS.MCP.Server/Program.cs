
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/", () => "IMS MCP Server is running!");

// If the process is started with --invoke-hello, call the HelloIms tool directly
// and write the result to stdout without starting the HTTP server. This is
// useful for automated invocation in the workspace.
if (args is not null && args.Contains("--invoke-hello"))
{
    Console.WriteLine(ImsTool.HelloIms());
    return;
}

app.MapMcp();

app.Run("http://localhost:3001");

[McpServerToolType]
public static class ImsTool
{
    [McpServerTool]
    [Description("Returns a greeting to verify that the IMS MCP tool is working.")]
    public static string HelloIms()
    {
        return "Hello from the IMS MCP Server!";
    }
}