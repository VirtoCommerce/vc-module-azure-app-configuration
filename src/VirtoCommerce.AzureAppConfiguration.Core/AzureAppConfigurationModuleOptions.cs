using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.AzureAppConfiguration.Core;

public class AzureAppConfigurationModuleOptions
{
    public const string SectionName = "AzureAppConfiguration";

    public bool Enabled { get; set; } = true;

    public string ConnectionString { get; set; }

    /// <summary>
    /// Azure App Configuration endpoint URI (used with Microsoft Entra ID authentication). For geo-replicated
    /// stores, prefer <see cref="Endpoints"/> to enable automatic failover across replicas.
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// Azure App Configuration replica endpoint URIs, in order of preference. The provider connects to the most
    /// preferred available replica and automatically fails over to the next one during an outage. Microsoft
    /// recommends geo-replication with failover as the primary resiliency mechanism. When set, this takes
    /// precedence over <see cref="Endpoint"/>. Applies to Entra ID authentication only (not connection strings).
    /// </summary>
    public string[] Endpoints { get; set; }

    /// <summary>
    /// Distributes requests across the configured replicas over time instead of always using the most preferred
    /// one, spreading load and avoiding exhaustion of a single replica's request quota. Only meaningful with
    /// multiple <see cref="Endpoints"/>. Default: <c>false</c>.
    /// </summary>
    public bool LoadBalancingEnabled { get; set; }

    /// <summary>
    /// Time-out for the initial configuration load at startup. The provider retries transient failures within
    /// this window before failing the application boot. When not set, the SDK default is used.
    /// </summary>
    public TimeSpan? StartupTimeout { get; set; }

    public string SentinelKey { get; set; } = "Sentinel";

    public TimeSpan? RefreshInterval { get; set; }

    public string KeyPrefix { get; set; }

    /// <summary>
    /// Credential type used to authenticate to Azure App Configuration and Key Vault.
    /// </summary>
    public AzureCredentialType CredentialType { get; set; } = AzureCredentialType.Default;

    /// <summary>
    /// Client ID of a user-assigned managed identity used to authenticate to Azure App Configuration
    /// and Azure Key Vault. Applies to both <see cref="AzureCredentialType.Default"/> and
    /// <see cref="AzureCredentialType.ManagedIdentity"/>. Leave empty to use a system-assigned identity
    /// (or, with <see cref="AzureCredentialType.Default"/>, the full credential chain).
    /// </summary>
    public string ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Options controlling resolution of Key Vault references stored in App Configuration.
    /// </summary>
    public KeyVaultOptions KeyVault { get; set; } = new();

    public bool HasConnectionString => !string.IsNullOrWhiteSpace(ConnectionString);

    public bool HasEndpoint => GetEndpoints().Count > 0;

    public bool IsConfigured => Enabled && (HasConnectionString || HasEndpoint);

    /// <summary>
    /// Returns the effective list of endpoints in preference order: <see cref="Endpoints"/> when provided,
    /// otherwise the single <see cref="Endpoint"/>.
    /// </summary>
    public IReadOnlyList<string> GetEndpoints()
    {
        if (Endpoints is { Length: > 0 })
        {
            return Endpoints.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
        }

        return string.IsNullOrWhiteSpace(Endpoint) ? [] : [Endpoint];
    }
}

public enum AzureCredentialType
{
    /// <summary>
    /// <c>DefaultAzureCredential</c> — probes managed identity, environment, and developer credentials in order.
    /// Works in both Azure and local development. Recommended default.
    /// </summary>
    Default,

    /// <summary>
    /// <c>ManagedIdentityCredential</c> — uses only the managed identity, skipping the credential-probing chain.
    /// Recommended for production deployments hosted in Azure (faster, no failed token attempts).
    /// </summary>
    ManagedIdentity,
}

public class KeyVaultOptions
{
    /// <summary>
    /// Enables resolution of Key Vault references stored in App Configuration. When enabled, the app reads
    /// the referenced secrets directly from Azure Key Vault using the configured credential. Default: <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval at which resolved Key Vault secrets are refreshed. When not set, secrets are cached for the
    /// application lifetime, even if the underlying secret is rotated in Key Vault.
    /// </summary>
    public TimeSpan? SecretRefreshInterval { get; set; }
}
