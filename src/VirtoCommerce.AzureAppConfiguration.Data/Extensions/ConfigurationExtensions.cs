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

        // Backward compatibility: fall back to legacy ConnectionStrings section
        if (!options.HasConnectionString)
        {
            var connectionString = configuration.GetConnectionString("AzureAppConfigurationConnectionString");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.ConnectionString = connectionString;
            }
        }

        return options;
    }
}
