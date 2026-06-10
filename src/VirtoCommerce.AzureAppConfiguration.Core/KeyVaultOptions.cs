using System;

namespace VirtoCommerce.AzureAppConfiguration.Core;

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
