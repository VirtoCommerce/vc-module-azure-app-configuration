using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VirtoCommerce.AzureAppConfiguration.Web.Extensions;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.AzureAppConfiguration.Web;

public class PlatformStartup : IPlatformStartup
{
    public int Priority => StartupPriority.ConfigurationSource;
    public PipelinePhase Phase => PipelinePhase.EarlyMiddleware;

    public void ConfigureAppConfiguration(IConfigurationBuilder builder, IHostEnvironment hostEnvironment)
    {
        var config = builder.Build();
        if (config.TryGetAzureAppConfigurationConnectionString(out var cs))
        {
            builder.AddAzureAppConfiguration(options =>
                options.Connect(cs)
                    .Select(KeyFilter.Any)
                    .Select(KeyFilter.Any, hostEnvironment.EnvironmentName)
                    .ConfigureRefresh(r => r.Register("Sentinel", refreshAll: true)));
        }
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.TryGetAzureAppConfigurationConnectionString(out _))
            services.AddAzureAppConfiguration();
    }

    public void Configure(IApplicationBuilder app, IConfiguration configuration)
    {
        if (configuration.TryGetAzureAppConfigurationConnectionString(out _))
            app.UseAzureAppConfiguration();
    }
}
