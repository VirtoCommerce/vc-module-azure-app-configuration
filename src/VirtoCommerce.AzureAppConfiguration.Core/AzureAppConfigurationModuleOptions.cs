using System;

namespace VirtoCommerce.AzureAppConfiguration.Core;

public class AzureAppConfigurationModuleOptions
{
    public const string SectionName = "AzureAppConfiguration";

    public bool Enabled { get; set; } = true;

    public string ConnectionString { get; set; }

    public string Endpoint { get; set; }

    public string SentinelKey { get; set; } = "Sentinel";

    public TimeSpan? RefreshInterval { get; set; }

    public string KeyPrefix { get; set; }

    /// <summary>
    /// Client ID of a user-assigned managed identity used to authenticate to Azure App Configuration
    /// and Azure Key Vault. When set, <c>DefaultAzureCredential</c> targets this identity. Leave empty to
    /// use the default credential chain (recommended for local development and system-assigned identity).
    /// </summary>
    public string ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Options controlling resolution of Key Vault references stored in App Configuration.
    /// </summary>
    public KeyVaultOptions KeyVault { get; set; } = new();

    public bool HasConnectionString => !string.IsNullOrWhiteSpace(ConnectionString);

    public bool HasEndpoint => !string.IsNullOrWhiteSpace(Endpoint);

    public bool IsConfigured => Enabled && (HasConnectionString || HasEndpoint);
}

public class KeyVaultOptions
{
    /// <summary>
    /// Enables resolution of Key Vault references stored in App Configuration. When enabled, the app reads
    /// the referenced secrets directly from Azure Key Vault using <c>DefaultAzureCredential</c>. Default: <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval at which resolved Key Vault secrets are refreshed. When not set, secrets are cached for the
    /// application lifetime, even if the underlying secret is rotated in Key Vault.
    /// </summary>
    public TimeSpan? SecretRefreshInterval { get; set; }
}
