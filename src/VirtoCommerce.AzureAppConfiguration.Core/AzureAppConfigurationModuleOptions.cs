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

    public bool HasConnectionString => !string.IsNullOrWhiteSpace(ConnectionString);

    public bool HasEndpoint => !string.IsNullOrWhiteSpace(Endpoint);

    public bool IsConfigured => Enabled && (HasConnectionString || HasEndpoint);
}
