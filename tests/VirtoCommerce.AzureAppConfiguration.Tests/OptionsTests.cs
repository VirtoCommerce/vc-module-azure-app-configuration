using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using VirtoCommerce.AzureAppConfiguration.Core;
using VirtoCommerce.AzureAppConfiguration.Data.Extensions;
using Xunit;

namespace VirtoCommerce.AzureAppConfiguration.Tests;

[Trait("Category", "Unit")]
public class OptionsTests
{
    [Fact]
    public void Defaults_KeyVaultEnabled_AndNoManagedIdentity()
    {
        var options = new AzureAppConfigurationModuleOptions();

        Assert.NotNull(options.KeyVault);
        Assert.True(options.KeyVault.Enabled);
        Assert.Null(options.KeyVault.SecretRefreshInterval);
        Assert.Null(options.ManagedIdentityClientId);
    }

    [Fact]
    public void GetOptions_BindsManagedIdentityAndKeyVaultSection()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string>
        {
            ["AzureAppConfiguration:Endpoint"] = "https://myconfig.azconfig.io",
            ["AzureAppConfiguration:ManagedIdentityClientId"] = "11111111-1111-1111-1111-111111111111",
            ["AzureAppConfiguration:KeyVault:Enabled"] = "false",
            ["AzureAppConfiguration:KeyVault:SecretRefreshInterval"] = "12:00:00",
        });

        var options = configuration.GetAzureAppConfigurationOptions();

        Assert.Equal("11111111-1111-1111-1111-111111111111", options.ManagedIdentityClientId);
        Assert.False(options.KeyVault.Enabled);
        Assert.Equal(TimeSpan.FromHours(12), options.KeyVault.SecretRefreshInterval);
    }

    [Fact]
    public void GetOptions_DefaultsKeyVaultEnabled_WhenSectionMissing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string>
        {
            ["AzureAppConfiguration:Endpoint"] = "https://myconfig.azconfig.io",
        });

        var options = configuration.GetAzureAppConfigurationOptions();

        Assert.True(options.KeyVault.Enabled);
        Assert.Null(options.KeyVault.SecretRefreshInterval);
        Assert.Null(options.ManagedIdentityClientId);
    }

    [Fact]
    public void Defaults_CredentialAndResiliency()
    {
        var options = new AzureAppConfigurationModuleOptions();

        Assert.Equal(AzureCredentialType.Default, options.CredentialType);
        Assert.False(options.LoadBalancingEnabled);
        Assert.Null(options.StartupTimeout);
        Assert.Empty(options.GetEndpoints());
    }

    [Fact]
    public void GetEndpoints_FallsBackToSingleEndpoint()
    {
        var options = new AzureAppConfigurationModuleOptions { Endpoint = "https://myconfig.azconfig.io" };

        Assert.Equal(["https://myconfig.azconfig.io"], options.GetEndpoints());
        Assert.True(options.HasEndpoint);
    }

    [Fact]
    public void GetEndpoints_PrefersEndpointsList_AndIgnoresBlanks()
    {
        var options = new AzureAppConfigurationModuleOptions
        {
            Endpoint = "https://ignored.azconfig.io",
            Endpoints = ["https://primary.azconfig.io", "  ", "https://secondary.azconfig.io"],
        };

        Assert.Equal(
            ["https://primary.azconfig.io", "https://secondary.azconfig.io"],
            options.GetEndpoints());
    }

    [Fact]
    public void GetOptions_BindsResiliencyAndCredentialType()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string>
        {
            ["AzureAppConfiguration:Endpoints:0"] = "https://primary.azconfig.io",
            ["AzureAppConfiguration:Endpoints:1"] = "https://secondary.azconfig.io",
            ["AzureAppConfiguration:LoadBalancingEnabled"] = "true",
            ["AzureAppConfiguration:StartupTimeout"] = "00:01:00",
            ["AzureAppConfiguration:CredentialType"] = "ManagedIdentity",
        });

        var options = configuration.GetAzureAppConfigurationOptions();

        Assert.Equal(
            ["https://primary.azconfig.io", "https://secondary.azconfig.io"],
            options.GetEndpoints());
        Assert.True(options.LoadBalancingEnabled);
        Assert.Equal(TimeSpan.FromMinutes(1), options.StartupTimeout);
        Assert.Equal(AzureCredentialType.ManagedIdentity, options.CredentialType);
        Assert.True(options.IsConfigured);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
