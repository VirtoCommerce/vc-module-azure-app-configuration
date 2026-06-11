using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.AzureAppConfiguration.Core;

namespace VirtoCommerce.AzureAppConfiguration.Data.HealthCheck;

public class AzureAppConfigurationHealthCheck : IHealthCheck
{
    private readonly AzureAppConfigurationModuleOptions _options;
    private readonly ILogger<AzureAppConfigurationHealthCheck> _logger;

    public AzureAppConfigurationHealthCheck(
        IOptions<AzureAppConfigurationModuleOptions> options,
        ILogger<AzureAppConfigurationHealthCheck> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured())
        {
            return HealthCheckResult.Healthy("Azure App Configuration is not configured");
        }

        try
        {
            var client = CreateClient();
            // Perform a lightweight read to verify connectivity
            await client.GetConfigurationSettingsAsync(new SettingSelector { KeyFilter = _options.SentinelKey }, cancellationToken)
                .AsPages()
                .GetAsyncEnumerator(cancellationToken)
                .MoveNextAsync();

            return HealthCheckResult.Healthy("Azure App Configuration is reachable");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure App Configuration health check failed");
            return HealthCheckResult.Degraded("Azure App Configuration is unreachable", ex);
        }
    }

    private ConfigurationClient CreateClient()
    {
        if (_options.HasConnectionString())
        {
            return new ConfigurationClient(_options.ConnectionString);
        }

        // Use the most preferred replica endpoint and the same credential as the configuration provider,
        // so the health check reflects the identity the application actually authenticates with.
        var endpoint = _options.GetEndpoints().First();

        return new ConfigurationClient(new Uri(endpoint), AzureCredentialFactory.Create(_options));
    }
}
