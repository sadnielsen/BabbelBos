using GnomeCatalog.Application;
using GnomeCatalog.Contracts;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GnomeCatalog.Infrastructure;

/// <summary>Registers the GnomeCatalog module in the host.</summary>
public static class GnomeCatalogModuleExtensions
{
    /// <summary>Configuration key for the prompts directory; defaults to <c>prompts</c> next to the app.</summary>
    public const string PromptsDirectoryKey = "Prompts:Directory";

    public static IServiceCollection AddGnomeCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        var directory = configuration[PromptsDirectoryKey] ?? Path.Combine(AppContext.BaseDirectory, "prompts");

        services.AddSingleton<IGnomeSource>(_ => new FileGnomeSource(directory));
        services.AddSingleton<IGnomeCatalog, GnomeCatalogService>();

        return services;
    }
}
