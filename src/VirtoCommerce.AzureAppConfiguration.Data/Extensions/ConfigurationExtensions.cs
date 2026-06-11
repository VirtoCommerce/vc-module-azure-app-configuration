using Microsoft.Extensions.Configuration;
using VirtoCommerce.AzureAppConfiguration.Core;

namespace VirtoCommerce.AzureAppConfiguration.Data.Extensions;

public static class ConfigurationExtensions
{
    public static bool TryGetAzureAppConfigurationConnectionString(this IConfiguration configuration, out string connectionString)
    {
        connectionString = configuration.GetConnectionString("AzureAppConfigurationConnectionString");
        return !string.IsNullOrWhiteSpace(connectionString);
    }

    public static AzureAppConfigurationModuleOptions GetAzureAppConfigurationOptions(this IConfiguration configuration)
    {
        var section = configuration.GetSection(AzureAppConfigurationModuleOptions.SectionName);
        var options = section.Get<AzureAppConfigurationModuleOptions>() ?? new AzureAppConfigurationModuleOptions();

        // Backward compatibility: the platform historically supplied the App Configuration connection string via
        // ConnectionStrings:AzureAppConfigurationConnectionString. Treat it as the primary source so existing
        // deployments keep working unchanged after the built-in support is moved into this module.
        if (configuration.TryGetAzureAppConfigurationConnectionString(out var connectionString))
        {
            options.ConnectionString = connectionString;
        }

        return options;
    }
}
