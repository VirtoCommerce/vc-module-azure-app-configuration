using System;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VirtoCommerce.AzureAppConfiguration.Core;
using VirtoCommerce.AzureAppConfiguration.Data.Extensions;
using VirtoCommerce.AzureAppConfiguration.Data.HealthCheck;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.AzureAppConfiguration.Web;

public class PlatformStartup : IPlatformStartup
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
    });

    private static readonly ILogger<PlatformStartup> _logger = _loggerFactory.CreateLogger<PlatformStartup>();

    public void ConfigureAppConfiguration(IConfigurationBuilder builder, IHostEnvironment env)
    {
        var config = builder.Build();
        var options = config.GetAzureAppConfigurationOptions();

        if (!options.Enabled)
        {
            _logger.LogInformation("Azure App Configuration is disabled via configuration");
            return;
        }

        if (!options.IsConfigured)
        {
            _logger.LogWarning("Azure App Configuration is not configured (no ConnectionString or Endpoint specified). Skipping");
            return;
        }

        builder.AddAzureAppConfiguration(azureOptions =>
        {
            // A single credential is shared between App Configuration and Key Vault, as Microsoft recommends
            // using the same managed identity for both. DefaultAzureCredential uses ManagedIdentityCredential
            // in Azure and developer credentials locally; ManagedIdentityClientId targets a user-assigned identity.
            var credential = CreateCredential(options);

            if (options.HasConnectionString)
            {
                _logger.LogDebug("Connecting to Azure App Configuration using connection string");
                azureOptions.Connect(options.ConnectionString);
            }
            else if (options.HasEndpoint)
            {
                _logger.LogDebug("Connecting to Azure App Configuration using DefaultAzureCredential at endpoint: {Endpoint}", options.Endpoint);
                azureOptions.Connect(new Uri(options.Endpoint), credential);
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

                _logger.LogDebug(
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

            _logger.LogDebug(
                "Azure App Configuration configured. {SentinelKey}, {KeyPrefix}, {RefreshInterval}",
                options.SentinelKey,
                options.KeyPrefix ?? "(Any)",
                options.RefreshInterval?.ToString() ?? "(default)");
        });
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetAzureAppConfigurationOptions();

        services.AddOptions<AzureAppConfigurationModuleOptions>()
            .Configure<IConfiguration>((opts, config) =>
            {
                config.GetSection(AzureAppConfigurationModuleOptions.SectionName).Bind(opts);

                if (!opts.HasConnectionString
                    && config.TryGetAzureAppConfigurationConnectionString(out var connectionString))
                {
                    opts.ConnectionString = connectionString;
                }
            });

        if (!options.IsConfigured)
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

        if (!options.IsConfigured)
        {
            return;
        }

        app.UseAzureAppConfiguration();

        _logger.LogInformation(
            "Azure App Configuration middleware is active. AuthMethod={AuthMethod}",
            options.HasConnectionString ? "ConnectionString" : "ManagedIdentity");
    }

    public void ConfigureHostServices(IServiceCollection services, IConfiguration config)
    {
    }

    private static DefaultAzureCredential CreateCredential(AzureAppConfigurationModuleOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ManagedIdentityClientId))
        {
            return new DefaultAzureCredential();
        }

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = options.ManagedIdentityClientId,
        });
    }
}
