using Microsoft.Extensions.Configuration;

namespace IFS.Automation.Config;

public static class ConfigurationHelper
{
    private static IConfigurationRoot? s_configuration;

    public static IConfigurationRoot Configuration =>
        s_configuration ??= new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
}
