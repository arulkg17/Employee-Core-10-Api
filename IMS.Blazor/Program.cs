using IMS.Blazor;
using IMS.Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ---------- API address (wwwroot/appsettings.json) ----------
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5063/api/";

if (!apiBase.EndsWith('/'))
{
    apiBase += "/"; // relative urls such as "v1/Customer/GetAll" need the trailing slash
}

// ---------- login state ----------
builder.Services.AddSingleton<TokenStore>();               // keeps the JWT (memory + browser localStorage)
builder.Services.AddTransient<JwtHandler>();               // adds "Authorization: Bearer ..." to every call
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddScoped<AuthService>();

// One HttpClient for the IMS API
builder.Services
    .AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBase);
        client.Timeout = TimeSpan.FromMinutes(5);
    })
    .AddHttpMessageHandler<JwtHandler>();

// ---------- API services ----------
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<VendorService>();
builder.Services.AddScoped<AppUserService>();
builder.Services.AddScoped<ItemmasterService>();
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<ReceiptService>();
builder.Services.AddScoped<SalesInvoiceService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<InvoiceAiService>();

// ---------- screen helpers ----------
builder.Services.AddScoped<ToastService>();

var host = builder.Build();

// read the saved token from localStorage BEFORE the first page is drawn
await host.Services.GetRequiredService<TokenStore>().InitializeAsync();

await host.RunAsync();
