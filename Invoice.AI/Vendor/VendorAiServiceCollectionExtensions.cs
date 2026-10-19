using Invoice.AI.Mcp;
using Invoice.AI.Vendor;

// Namespace chosen so the host needs no extra "using" for services.AddVendorAi(...).
namespace Microsoft.Extensions.DependencyInjection;

public static class VendorAiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the vendor question service of the Invoice.AI library. The Kernel itself must already be registered by your
    /// existing Invoice.AI setup (for example AddKernel() with the Ollama chat completion).
    /// </summary>
    public static IServiceCollection AddVendorAi(this IServiceCollection services, Action<ImsMcpOptions>? configure = null)
    {
        services.AddOptions<ImsMcpOptions>();
        if (configure is not null) services.Configure(configure);

        services.AddSingleton<SkillPromptProvider>();
        // Default MCP session factory used in production. Tests can replace IImsMcpSessionFactory.
        services.AddSingleton<IImsMcpSessionFactory, DefaultImsMcpSessionFactory>();
        services.AddScoped<VendorAskService>();
        return services;
    }
}
