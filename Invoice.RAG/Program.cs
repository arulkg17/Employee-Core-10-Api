using System.Text;
using Invoice.RAG.Agent;
using Invoice.RAG.Configuration;
using Invoice.RAG.Models;
using Invoice.RAG.Ollama;
using Invoice.RAG.Retrieval;
using Invoice.RAG.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));
var rag = builder.Configuration.GetSection(RagOptions.SectionName).Get<RagOptions>() ?? new RagOptions();

// ---- Authentication: validates the same JWT that Invoice CoreAPI issues ----
// ============================================================
// JWT Authentication
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Audience is not configured.");
}

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                ClockSkew = TimeSpan.Zero
            };
    });


// ============================================================
// Authorization
// ============================================================

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
// To get Swagger UI, copy the AddSwaggerGen / Bearer setup from Invoice.CoreAPI (kept out here to avoid package-version clashes).

// ---- Upload limits ----
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = rag.MaxUploadBytes);
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = rag.MaxUploadBytes + 1_048_576);

// ---- Ollama (local models) ----
builder.Services.AddHttpClient<OllamaClient>((sp, client) =>
{
    var o = sp.GetRequiredService<IOptions<RagOptions>>().Value.Ollama;
    client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
});

// ---- CoreAPI (vendors, purchase orders) - the caller's JWT is forwarded ----
builder.Services.AddTransient<ForwardJwtHandler>();
builder.Services.AddHttpClient<CoreApiClient>(client =>
{
    var baseUrl = builder.Configuration["CoreApi:BaseUrl"] ?? "http://localhost:5063/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<ForwardJwtHandler>();

// ---- Agreement store: in-memory for a first try, SQL Server for real use ----
if (rag.StoreProvider.Equals("Sql", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IAgreementStore, SqlAgreementStore>();
else
    builder.Services.AddSingleton<IAgreementStore, InMemoryAgreementStore>();

builder.Services.AddScoped<Retriever>();
builder.Services.AddScoped<AgreementIngestionService>();
builder.Services.AddScoped<AgreementTermsService>();
builder.Services.AddScoped<RagAnswerService>();
builder.Services.AddScoped<AgentToolbox>();   // per request: keeps the numbered citations of one question
builder.Services.AddScoped<AgentService>();
// ===== Invoice.RAG / Program.cs : add next to the other AddScoped lines =====
builder.Services.AddScoped<Invoice.RAG.Policies.PolicyIndex>();
//==================================
//  Swagger Configuration
//===============================================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Invoice Core API",
        Version = "v1",
        Description = "Invoice Management Core API"
    });

    // JWT Bearer Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =
            "Enter your JWT token.\r\n\r\n" +
            "Example: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    // Tell Swagger to send the Bearer token with secured API operations.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();

// One place that turns exceptions into clean JSON (no stack traces or internal messages for the caller).
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    if (context.RequestAborted.IsCancellationRequested) return;

    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, message) = ex switch
    {
        RagException r => (r.StatusCode, r.Message),
        HttpRequestException => (503, "The AI model is not reachable. Check that Ollama is running."),
        TaskCanceledException => (504, "The AI model took too long. Try again or raise Rag:Ollama:TimeoutSeconds."),
        _ => (500, "An unexpected error occurred.")
    };

    if (status >= 500 && ex is not null)
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Invoice.RAG").LogError(ex, "Request failed");

    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new ApiResult<object>(false, message));
}));

// ============================================================
// Swagger
// ============================================================

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Invoice Core API V1");

    options.RoutePrefix = "swagger";
});


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", store = rag.StoreProvider })).AllowAnonymous();

app.Run();
