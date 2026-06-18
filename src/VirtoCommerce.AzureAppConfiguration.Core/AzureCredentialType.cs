namespace VirtoCommerce.AzureAppConfiguration.Core;

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
