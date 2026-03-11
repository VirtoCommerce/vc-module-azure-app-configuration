# VirtoCommerce Azure App Configuration Module

## Overview

The **Azure App Configuration** module integrates [Microsoft Azure App Configuration](https://learn.microsoft.com/en-us/azure/azure-app-configuration/overview) service into the VirtoCommerce platform. It allows you to externalize storage and management of application settings, replacing or augmenting local `appsettings.json` files with a centralized, cloud-managed configuration store.

This module does not replace any existing module. It acts as an optional configuration source that layers on top of the standard .NET configuration pipeline. Once installed, all platform modules automatically gain access to settings stored in Azure App Configuration without any code changes.

### When to use this module

- **Centralized configuration management** — manage settings for multiple VirtoCommerce instances from a single Azure App Configuration resource.
- **Multi-environment deployments** — use label-based filtering to serve different settings per environment (Development, Staging, Production) from one configuration store.
- **Dynamic configuration updates** — change application settings at runtime without redeploying or restarting the platform, using the Sentinel key refresh mechanism.
- **Azure-native infrastructure** — leverage Azure's built-in security, RBAC, encryption, and audit capabilities for configuration data.

## Key Features

- **Early configuration pipeline integration** — registers as a `ConfigurationSource` at the highest startup priority, ensuring Azure-managed settings are available before any module initialization.
- **Environment-aware key filtering** — automatically loads base keys (`KeyFilter.Any`) and environment-specific keys labeled with `IHostEnvironment.EnvironmentName`, allowing environment overrides.
- **Sentinel-based configuration refresh** — monitors a configurable Sentinel key; when its value changes, all configuration entries are refreshed without application restart.
- **Zero-code adoption** — no changes required in other modules; settings resolved through `IConfiguration` and `IOptions<T>` automatically pick up values from Azure App Configuration.

## Configuration

### Prerequisites

1. An [Azure App Configuration](https://learn.microsoft.com/en-us/azure/azure-app-configuration/quickstart-azure-app-configuration-create) resource in your Azure subscription.
2. A connection string with read access to the resource.

### Connection string

Provide the Azure App Configuration connection string through any standard .NET configuration source (environment variable, Key Vault reference, or `appsettings.json`):

```json
{
  "ConnectionStrings": {
    "AzureAppConfiguration": "Endpoint=https://<your-resource>.azconfig.io;Id=<id>;Secret=<secret>"
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

The module registers a **Sentinel** key for configuration refresh. To trigger a reload of all settings at runtime:

1. Create a key named `Sentinel` in your Azure App Configuration resource.
2. When you need to refresh settings, update the `Sentinel` value (any change triggers a full reload).

## Documentation

- [Azure App Configuration overview](https://learn.microsoft.com/en-us/azure/azure-app-configuration/overview)
- [Use dynamic configuration in ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-app-configuration/enable-dynamic-configuration-aspnet-core)
- [Azure App Configuration best practices](https://learn.microsoft.com/en-us/azure/azure-app-configuration/howto-best-practices)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-catalog/releases/latest)

## License

Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense
