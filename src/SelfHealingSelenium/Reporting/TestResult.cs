namespace SelfHealingSelenium.Reporting;

public sealed class TestResult
{
    public required string FieldKey { get; init; }
    public required string DisplayName { get; init; }
    public bool Passed { get; set; }
    public string Details { get; set; } = string.Empty;
    public string LocatorStrategy { get; set; } = string.Empty;
    public string LocatorValue { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
}
