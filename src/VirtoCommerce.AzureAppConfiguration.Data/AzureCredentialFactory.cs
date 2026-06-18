using Azure.Core;
using Azure.Identity;
using VirtoCommerce.AzureAppConfiguration.Core;

namespace VirtoCommerce.AzureAppConfiguration.Data;

/// <summary>
/// Builds the <see cref="TokenCredential"/> used to authenticate to Azure App Configuration and Key Vault,
/// honoring <see cref="AzureAppConfigurationModuleOptions.CredentialType"/> and
/// <see cref="AzureAppConfigurationModuleOptions.ManagedIdentityClientId"/>. Shared by the configuration
/// provider and the health check so both authenticate with the same identity.
/// </summary>
public static class AzureCredentialFactory
{
    public static TokenCredential Create(AzureAppConfigurationModuleOptions options)
    {
        var hasClientId = !string.IsNullOrWhiteSpace(options.ManagedIdentityClientId);

        // ManagedIdentityCredential skips the DefaultAzureCredential probing chain — recommended for
        // production workloads hosted in Azure (lower latency, no failed token attempts).
        if (options.CredentialType == AzureCredentialType.ManagedIdentity)
        {
            var managedIdentityId = hasClientId
                ? ManagedIdentityId.FromUserAssignedClientId(options.ManagedIdentityClientId)
                : ManagedIdentityId.SystemAssigned;

            return new ManagedIdentityCredential(managedIdentityId);
        }

        if (!hasClientId)
        {
            return new DefaultAzureCredential();
        }

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = options.ManagedIdentityClientId,
        });
    }
}
