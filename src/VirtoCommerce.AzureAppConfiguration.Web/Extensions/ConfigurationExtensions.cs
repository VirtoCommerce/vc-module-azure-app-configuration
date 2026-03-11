using Microsoft.Extensions.Configuration;

namespace VirtoCommerce.AzureAppConfiguration.Web.Extensions;

public static class ConfigurationExtensions
{
    public static bool TryGetAzureAppConfigurationConnectionString(this IConfiguration configuration, out string connectionString)
    {
        connectionString = configuration.GetConnectionString("AzureAppConfigurationConnectionString");
        return !string.IsNullOrWhiteSpace(connectionString);
    }
}
