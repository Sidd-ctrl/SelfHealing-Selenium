using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using SelfHealingSelenium.Configuration;

namespace SelfHealingSelenium.Automation;

public static class DriverFactory
{
    public static IWebDriver Create(AppSettings settings)
    {
        IWebDriver driver = settings.Browser.Trim().Equals("edge", StringComparison.OrdinalIgnoreCase)
            ? CreateEdgeDriver(settings)
            : CreateChromeDriver(settings);

        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(settings.ImplicitWaitSeconds);
        driver.Manage().Window.Maximize();
        return driver;
    }

    private static IWebDriver CreateChromeDriver(AppSettings settings)
    {
        var options = new ChromeOptions();
        if (settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1440,900");
        return new ChromeDriver(options);
    }

    private static IWebDriver CreateEdgeDriver(AppSettings settings)
    {
        var options = new EdgeOptions();
        if (!string.IsNullOrWhiteSpace(settings.BrowserPath))
        {
            options.BinaryLocation = settings.BrowserPath;
        }

        if (settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1440,900");
        return new EdgeDriver(options);
    }
}
