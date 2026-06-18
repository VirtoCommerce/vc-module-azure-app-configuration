using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VirtoCommerce.AzureAppConfiguration.Core;
using VirtoCommerce.AzureAppConfiguration.Data;
using VirtoCommerce.AzureAppConfiguration.Data.Extensions;
using VirtoCommerce.AzureAppConfiguration.Data.HealthCheck;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.AzureAppConfiguration.Web;

public class PlatformStartup : IPlatformStartup, IHasLogger
{
    public ILogger Logger { get; set; }

    public void ConfigureAppConfiguration(IConfigurationBuilder builder, IHostEnvironment env)
    {
        var config = builder.Build();
        var options = config.GetAzureAppConfigurationOptions();

        if (!options.Enabled)
        {
            Logger.LogInformation("Azure App Configuration is disabled via configuration");
            return;
        }

        if (!options.IsConfigured())
        {
            Logger.LogWarning("Azure App Configuration is not configured (no ConnectionString or Endpoint specified). Skipping");
            return;
        }

        builder.AddAzureAppConfiguration(azureOptions =>
        {
            // A single credential is shared between App Configuration and Key Vault, as Microsoft recommends
            // using the same managed identity for both. DefaultAzureCredential uses ManagedIdentityCredential
            // in Azure and developer credentials locally; ManagedIdentityClientId targets a user-assigned identity.
            var credential = AzureCredentialFactory.Create(options);

            if (options.HasConnectionString())
            {
                Logger.LogDebug("Connecting to Azure App Configuration using connection string");
                azureOptions.Connect(options.ConnectionString);
            }
            else if (options.HasEndpoints())
            {
                var endpoints = options.GetEndpoints().Select(e => new Uri(e)).ToArray();

                Logger.LogDebug(
                    "Connecting to Azure App Configuration using {CredentialType} at {EndpointCount} endpoint(s): {Endpoints}",
                    options.CredentialType,
                    endpoints.Length,
                    string.Join(", ", endpoints));

                // Pass all replica endpoints in preference order so the provider can automatically fail over
                // between them during an outage, as Microsoft recommends for geo-replicated stores.
                azureOptions.Connect(endpoints, credential);

                // Spread requests across replicas over time to avoid exhausting a single replica's quota.
                azureOptions.LoadBalancingEnabled = options.LoadBalancingEnabled;
            }

            if (options.StartupTimeout.HasValue)
            {
                // Bound the initial configuration load. The provider retries transient failures within this
                // window before failing the platform boot — important because this module loads before all others.
                azureOptions.ConfigureStartupOptions(startup => startup.Timeout = options.StartupTimeout.Value);
            }

            if (options.KeyVault.Enabled)
            {
                // Resolve Key Vault references stored in App Configuration. The app reads secrets from Key Vault
                // directly, so a credential is required here regardless of how App Configuration itself authenticates.
                azureOptions.ConfigureKeyVault(keyVaultOptions =>
                {
                    keyVaultOptions.SetCredential(credential);

                    if (options.KeyVault.SecretRefreshInterval.HasValue)
                    {
                        keyVaultOptions.SetSecretRefreshInterval(options.KeyVault.SecretRefreshInterval.Value);
                    }
                });

                Logger.LogDebug(
                    "Azure Key Vault reference resolution enabled. {SecretRefreshInterval}",
                    options.KeyVault.SecretRefreshInterval?.ToString() ?? "(no refresh)");
            }

            var keyFilter = string.IsNullOrWhiteSpace(options.KeyPrefix)
                ? KeyFilter.Any
                : options.KeyPrefix + "*";

            azureOptions
                .Select(keyFilter)
                .Select(keyFilter, env.EnvironmentName);

            if (!string.IsNullOrWhiteSpace(options.KeyPrefix))
            {
                azureOptions.TrimKeyPrefix(options.KeyPrefix);
            }

            azureOptions.ConfigureRefresh(refresh =>
            {
                refresh.Register(options.SentinelKey, refreshAll: true);

                if (options.RefreshInterval.HasValue)
                {
                    refresh.SetRefreshInterval(options.RefreshInterval.Value);
                }
            });

            Logger.LogDebug(
                "Azure App Configuration configured. {SentinelKey}, {KeyPrefix}, {RefreshInterval}",
                options.SentinelKey,
                options.KeyPrefix ?? "(Any)",
                options.RefreshInterval?.ToString() ?? "(default)");
        },
        optional: options.Optional);
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AzureAppConfigurationModuleOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(AzureAppConfigurationModuleOptions.SectionName).Bind(options);

                // Backward compatibility: the legacy platform connection string takes precedence (see GetAzureAppConfigurationOptions).
                if (config.TryGetAzureAppConfigurationConnectionString(out var connectionString))
                {
                    options.ConnectionString = connectionString;
                }
            });

        var options = configuration.GetAzureAppConfigurationOptions();
        if (!options.IsConfigured())
        {
            return;
        }

        services.AddAzureAppConfiguration();

        services.AddHealthChecks()
            .AddCheck<AzureAppConfigurationHealthCheck>(
                "AzureAppConfiguration",
                failureStatus: HealthStatus.Degraded,
                tags: ["infrastructure", "azure"]);
    }

    public void Configure(IApplicationBuilder app, IConfiguration configuration)
    {
        var options = configuration.GetAzureAppConfigurationOptions();

        if (!options.IsConfigured())
        {
            return;
        }

        app.UseAzureAppConfiguration();

        Logger.LogInformation(
            "Azure App Configuration middleware is active. AuthMethod={AuthMethod}",
            options.HasConnectionString() ? "ConnectionString" : options.CredentialType.ToString());
    }

    public void ConfigureHostServices(IServiceCollection services, IConfiguration config)
    {
    }
}
