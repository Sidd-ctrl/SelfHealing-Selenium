using OpenQA.Selenium;
using SelfHealingSelenium.Automation;
using SelfHealingSelenium.Configuration;
using SelfHealingSelenium.Locators;
using SelfHealingSelenium.Reporting;

namespace SelfHealingSelenium;

public static class Program
{
    public static int Main(string[] args)
    {
        AppSettings settings;
        try
        {
            settings = SettingsLoader.Load(args);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Configuration error: {error.Message}");
            return 2;
        }

        TestReporter.Log($"Starting self-healing form automation against: {settings.TargetUrl}");
        TestReporter.Log($"Browser={settings.Browser} Headless={settings.Headless} ConfidenceThreshold={settings.ConfidenceThreshold}");

        IWebDriver? driver = null;
        var results = new List<TestResult>();

        try
        {
            driver = DriverFactory.Create(settings);
            TestReporter.Log("WebDriver started successfully.");

            if (Uri.TryCreate(settings.TargetUrl, UriKind.Absolute, out var uri) && uri.Scheme == "file")
            {
                TestReporter.Log($"Loading local fixture: {uri.LocalPath}");
            }

            driver.Navigate().GoToUrl(settings.TargetUrl);
            TestReporter.Log($"Navigated to {driver.Url}");

            var locatorEngine = new SmartLocatorEngine(driver, settings.ExplicitWaitSeconds, settings.ConfidenceThreshold);
            var automation = new FormAutomation(driver, locatorEngine, settings.ExplicitWaitSeconds, settings.MaxRetryAttempts);

            results.Add(automation.TestFirstName());
            results.Add(automation.TestGender());
            results.Add(automation.TestState());
        }
        catch (Exception error)
        {
            TestReporter.Log($"Fatal error during execution: {error}");
            results.Add(new TestResult
            {
                FieldKey = "driver",
                DisplayName = "Driver setup / navigation",
                Passed = false,
                Details = error.Message
            });
        }
        finally
        {
            try
            {
                driver?.Quit();
                driver?.Dispose();
                TestReporter.Log("WebDriver closed.");
            }
            catch (Exception error)
            {
                TestReporter.Log($"Error while closing WebDriver: {error.Message}");
            }
        }

        TestReporter.PrintSummary(results);
        return results.All(r => r.Passed) ? 0 : 1;
    }
}
