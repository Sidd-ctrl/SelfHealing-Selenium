namespace SelfHealingSelenium.Configuration;

public sealed class AppSettings
{
    public string TargetUrl { get; set; } = "https://app.cloudqa.io/home/AutomationPracticeForm";
    public string Browser { get; set; } = "Chrome";
    public string BrowserPath { get; set; } = string.Empty;
    public bool Headless { get; set; } = true;
    public int ImplicitWaitSeconds { get; set; } = 2;
    public int ExplicitWaitSeconds { get; set; } = 15;
    public double ConfidenceThreshold { get; set; } = 0.55;
    public int MaxRetryAttempts { get; set; } = 3;
    public string FixturesDirectory { get; set; } = "Fixtures";
}
