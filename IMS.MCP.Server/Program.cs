using IMS.MCP.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

// Lets tools read the incoming MCP request (e.g. the Authorization header).
builder.Services.AddHttpContextAccessor();

// Typed client for the Invoice CoreAPI (port 5063).
builder.Services.AddHttpClient("InvoiceApi", client =>
{
    var baseUrl = builder.Configuration["InvoiceApi:BaseUrl"]
        ?? "http://localhost:5063/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Shared helper used by every tool class.
builder.Services.AddSingleton<InvoiceApiClient>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Plain health check on its own path so it never collides with MCP.
app.MapGet("/health", () => "IMS MCP Server is running!");

// MCP endpoint -> http://localhost:3001/mcp
app.MapMcp("/mcp");

app.Run("http://localhost:3001");

[McpServerToolType]
public static class ImsTool
{
    [McpServerTool(Name = "hello_ims", ReadOnly = true)]
    [Description("Returns a greeting to verify that the IMS MCP tool is working.")]
    public static string HelloIms() => "Hello from the IMS MCP Server!";
}
