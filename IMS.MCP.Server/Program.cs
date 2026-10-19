using IMS.MCP.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using IMS.MCP.Server.Clients;
using IMS.MCP.Server.Skills;

var builder = WebApplication.CreateBuilder(args);

// Lets tools read the incoming MCP request (e.g. the Authorization header).
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardAuthHandler>();

//=====================================================================
// 1. The skill catalog (reads the Skills folder).
//=====================================================================
builder.Services.AddSingleton<SkillCatalog>();

//=====================================================================
// 2. Your existing AddMcpServer call: add the instructions text and WithPromptsFromAssembly().
//    Instructions are sent to the client when it connects, so even a client that never calls list_skills knows the skills exist.
//=====================================================================
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInstructions =
            "IMS vendor and purchase-to-pay server. For questions about purchase orders, goods receipts, vendor invoices or vendor payments, " +
            "call get_skill with name 'ims-purchase-to-pay' first and follow it. Vendor and PO facts come from find_vendor, " +
            "get_vendor_purchase_orders and get_purchase_order; agreement wording from search_vendor_agreements; " +
            "dates and money from get_po_payment_summary; approval levels and invoice matching from get_approval_level and evaluate_three_way_match.";
    })
    .WithHttpTransport()              // keep whatever you already have here
    .WithToolsFromAssembly()          // picks up SkillTools and P2pRuleTools too
    .WithPromptsFromAssembly();       // new: P2pPrompts

//=====================================================================
// Structured data: Invoice CoreAPI (Accounts database through DAL/BAL)
//=====================================================================

builder.Services.AddHttpClient(ImsApi.Core, client =>
{
    var baseUrl = builder.Configuration["Ims:CoreApiBaseUrl"] ?? "http://localhost:5063/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<ForwardAuthHandler>();

//================================================================================================================
// Unstructured data: Invoice.RAG API (InvoiceRag database). Long timeout: ask_vendor_questions runs a local model.
//=================================================================================================================
builder.Services.AddHttpClient(ImsApi.Rag, client =>
{
    var baseUrl = builder.Configuration["Ims:RagApiBaseUrl"] ?? "http://localhost:5070/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(300);
}).AddHttpMessageHandler<ForwardAuthHandler>();

builder.Services.AddTransient<ImsApi>();


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

//builder.Services
//    .AddMcpServer()
//    .WithHttpTransport()
//    .WithToolsFromAssembly();

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
