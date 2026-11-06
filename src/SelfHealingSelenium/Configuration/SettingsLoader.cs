using Microsoft.Extensions.Configuration;

namespace SelfHealingSelenium.Configuration;

public static class SettingsLoader
{
    public static AppSettings Load(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var settings = new AppSettings();
        configuration.Bind(settings);

        if (string.IsNullOrWhiteSpace(settings.TargetUrl))
        {
            throw new InvalidOperationException("TargetUrl is not configured. Pass --TargetUrl <url> or set it in appsettings.json.");
        }

        return settings;
    }
}
