# Azure App Configuration Module

## Overview

The **Azure App Configuration** module integrates [Microsoft Azure App Configuration](https://learn.microsoft.com/en-us/azure/azure-app-configuration/overview) service into the VirtoCommerce platform. It allows you to externalize storage and management of application settings, replacing or augmenting local `appsettings.json` files with a centralized, cloud-managed configuration store.

This module does not replace any existing module. It acts as an optional configuration source that layers on top of the standard .NET configuration pipeline. Once installed, all platform modules automatically gain access to settings stored in Azure App Configuration without any code changes.

### When to use this module

- **Centralized configuration management** — manage settings for multiple VirtoCommerce instances from a single Azure App Configuration resource.
- **Multi-environment deployments** — use label-based filtering to serve different settings per environment (Development, Staging, Production) from one configuration store.
- **Dynamic configuration updates** — change application settings at runtime without redeploying or restarting the platform, using the Sentinel key refresh mechanism.
- **Azure-native infrastructure** — leverage Azure's built-in security, RBAC, encryption, and audit capabilities for configuration data.

## Key Features

- **Managed Identity & connection string authentication** — supports both `DefaultAzureCredential` (recommended for Azure-hosted apps) and connection string authentication.
- **Early configuration pipeline integration** — registers as a `ConfigurationSource` at the highest startup priority, ensuring Azure-managed settings are available before any module initialization.
- **Environment-aware key filtering** — automatically loads base keys and environment-specific keys labeled with `IHostEnvironment.EnvironmentName`, allowing environment overrides.
- **Key prefix filtering** — optionally filter and trim key prefixes to scope configuration to your application.
- **Sentinel-based configuration refresh** — monitors a configurable Sentinel key; when its value changes, all configuration entries are refreshed without application restart.
- **Configurable refresh interval** — control how frequently the module polls for configuration changes.
- **Health check** — built-in ASP.NET Core health check for monitoring Azure App Configuration connectivity.
- **Structured logging** — logs authentication method, configuration status, and refresh events at startup.
- **Zero-code adoption** — no changes required in other modules; settings resolved through `IConfiguration` and `IOptions<T>` automatically pick up values from Azure App Configuration.

## Configuration

### Prerequisites

1. An [Azure App Configuration](https://learn.microsoft.com/en-us/azure/azure-app-configuration/quickstart-azure-app-configuration-create) resource in your Azure subscription.
2. Either a connection string or a Managed Identity with read access to the resource.

### Authentication

The module supports two authentication methods:

**Option A: Connection string**

```json
{
  "AzureAppConfiguration": {
    "ConnectionString": "Endpoint=https://<your-resource>.azconfig.io;Id=<id>;Secret=<secret>"
  }
}
```

**Option B: Legacy connection string (backward compatible)**

```json
{
  "ConnectionStrings": {
    "AzureAppConfigurationConnectionString": "Endpoint=https://<your-resource>.azconfig.io;Id=<id>;Secret=<secret>"
  }
}
```

If both `ConnectionString` and `Endpoint` are specified, `ConnectionString` takes priority.

### Module options

All options are configured under the `AzureAppConfiguration` section in `appsettings.json`:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable or disable the module |
| `ConnectionString` | `string` | — | Azure App Configuration connection string |
| `Endpoint` | `string` | — | Azure App Configuration endpoint URI (for Managed Identity) |
| `SentinelKey` | `string` | `"Sentinel"` | Key name used to trigger configuration refresh |
| `RefreshInterval` | `TimeSpan` | SDK default (30s) | How often to poll for configuration changes |
| `KeyPrefix` | `string` | — | Filter keys by prefix and trim it from loaded keys |

**Full example:**

```json
{
  "AzureAppConfiguration": {
    "Endpoint": "https://myconfig.azconfig.io",
    "SentinelKey": "Sentinel",
    "RefreshInterval": "00:02:00",
    "KeyPrefix": "VirtoCommerce:",
    "Enabled": true
  }
}
```

### Environment labels

Keys stored in Azure App Configuration can be labeled with the target environment name. The module automatically selects keys matching the current `ASPNETCORE_ENVIRONMENT` value:

| Label | Loaded when |
|-------|------------|
| *(no label)* | Always (base/default settings) |
| `Development` | `ASPNETCORE_ENVIRONMENT=Development` |
| `Staging` | `ASPNETCORE_ENVIRONMENT=Staging` |
| `Production` | `ASPNETCORE_ENVIRONMENT=Production` |

Environment-labeled keys take precedence over unlabeled keys.

### Configuration refresh

The module monitors a **Sentinel** key for configuration refresh. To trigger a reload of all settings at runtime:

1. Create a key named `Sentinel` (or your custom `SentinelKey` value) in your Azure App Configuration resource.
2. When you need to refresh settings, update the Sentinel value (any change triggers a full reload).

### Health check

The module registers an ASP.NET Core health check named `AzureAppConfiguration` with tags `infrastructure` and `azure`. It reports `Degraded` (not `Unhealthy`) when Azure App Configuration is unreachable, since the platform can still function with cached configuration.

### Troubleshooting

The module emits structured log messages under the `VirtoCommerce.AzureAppConfiguration.Web.PlatformStartup` category. Connection details (auth method, SentinelKey, KeyPrefix, RefreshInterval) are logged at `Debug` level and are **not visible** at the default `Information` level.

To enable verbose logging for troubleshooting, add the following to `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "VirtoCommerce.AzureAppConfiguration.Web.PlatformStartup": "Debug"
    }
  }
}
```

**Log messages reference:**

| Level | Message | When |
|-------|---------|------|
| Information | Azure App Configuration is disabled via configuration | `Enabled` is `false` |
| Warning | Azure App Configuration is not configured | No `ConnectionString` or `Endpoint` provided |
| Debug | Connecting to Azure App Configuration using connection string | Connection string auth selected |
| Debug | Connecting to Azure App Configuration using DefaultAzureCredential at endpoint: {Endpoint} | Managed Identity auth selected |
| Debug | Azure App Configuration configured. {SentinelKey}, {KeyPrefix}, {RefreshInterval} | Provider successfully registered |
| Information | Azure App Configuration middleware is active. AuthMethod={AuthMethod} | Middleware pipeline is ready |

## Documentation

- [Azure App Configuration overview](https://learn.microsoft.com/en-us/azure/azure-app-configuration/overview)
- [Use dynamic configuration in ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-aspnet-core)
- [Azure App Configuration best practices](https://learn.microsoft.com/en-us/azure/azure-app-configuration/howto-best-practices)
- [Use managed identities to access App Configuration](https://learn.microsoft.com/en-us/azure/azure-app-configuration/howto-integrate-azure-managed-service-identity)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-azure-app-configuration/releases/latest)

## License

Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense
