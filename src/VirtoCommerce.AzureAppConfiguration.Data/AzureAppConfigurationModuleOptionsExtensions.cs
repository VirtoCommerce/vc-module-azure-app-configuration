using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.AzureAppConfiguration.Core;

namespace VirtoCommerce.AzureAppConfiguration.Data;

/// <summary>
/// Derived logic over <see cref="AzureAppConfigurationModuleOptions"/>. Kept out of the Core options POCO so
/// that type stays a plain configuration-binding target.
/// </summary>
public static class AzureAppConfigurationModuleOptionsExtensions
{
    public static bool HasConnectionString(this AzureAppConfigurationModuleOptions options) =>
        !string.IsNullOrWhiteSpace(options.ConnectionString);

    public static bool HasEndpoints(this AzureAppConfigurationModuleOptions options) =>
        options.GetEndpoints().Count > 0;

    public static bool IsConfigured(this AzureAppConfigurationModuleOptions options) =>
        options.Enabled && (options.HasConnectionString() || options.HasEndpoints());

    /// <summary>
    /// Returns the effective list of endpoints in preference order: <see cref="AzureAppConfigurationModuleOptions.Endpoints"/>
    /// when provided, otherwise the single <see cref="AzureAppConfigurationModuleOptions.Endpoint"/>.
    /// </summary>
    public static IList<string> GetEndpoints(this AzureAppConfigurationModuleOptions options)
    {
        if (options.Endpoints is { Length: > 0 })
        {
            return options.Endpoints.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        }

        return string.IsNullOrWhiteSpace(options.Endpoint) ? [] : [options.Endpoint];
    }
}
