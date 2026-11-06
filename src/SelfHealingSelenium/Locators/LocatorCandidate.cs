using OpenQA.Selenium;

namespace SelfHealingSelenium.Locators;

public sealed class LocatorCandidate
{
    public required IWebElement Element { get; init; }
    public double Score { get; set; }
    public string MatchedStrategy { get; set; } = string.Empty;
    public string MatchedValue { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
}
