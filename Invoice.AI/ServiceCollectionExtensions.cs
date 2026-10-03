using Invoice.AI.Abstractions;
using Invoice.AI.Configuration;
using Invoice.AI.Data;
using Invoice.AI.Intents;
using Invoice.AI.Intents.Handlers;
using Invoice.AI.Ollama;
using Invoice.AI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Invoice.AI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// One line in Program.cs:  builder.Services.AddInvoiceAI(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddInvoiceAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InvoiceAIOptions>(configuration.GetSection(InvoiceAIOptions.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));

        // Typed HttpClient for Ollama (base address + long timeout for slow local models)
        services.AddHttpClient<IOllamaClient, OllamaClient>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
        });

        // Data source: fake in-memory data (demo/tests) or real SQL Server
        var useInMemory = configuration.GetValue<bool>($"{InvoiceAIOptions.SectionName}:{nameof(InvoiceAIOptions.UseInMemoryData)}");
        if (useInMemory)
            services.AddSingleton<IMasterDataQueryService, InMemoryMasterDataQueryService>();
        else
            services.AddScoped<IMasterDataQueryService, SqlMasterDataQueryService>();

        // One handler per business question type. Add a new line here to teach the AI something new.
        services.AddScoped<IIntentHandler, CategoryItemCountHandler>();
        services.AddScoped<IIntentHandler, CategoryCountHandler>();
        services.AddScoped<IIntentHandler, ItemCountHandler>();
        services.AddScoped<IIntentHandler, CustomerCountHandler>();
        services.AddScoped<IIntentHandler, CustomerCountByCityHandler>();
        services.AddScoped<IIntentHandler, CustomerCountAllCitiesHandler>();
        services.AddScoped<IIntentHandler, VendorCountHandler>();
        services.AddScoped<IIntentHandler, UserCountHandler>();

        services.AddScoped<IntentCatalog>();
        services.AddSingleton<IntentPromptBuilder>();
        services.AddScoped<IntentExtractor>();
        services.AddScoped<IAIOrchestrator, AIOrchestrator>();

        return services;
    }
}
