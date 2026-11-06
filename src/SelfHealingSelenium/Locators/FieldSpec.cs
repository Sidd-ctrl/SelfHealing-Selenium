namespace SelfHealingSelenium.Locators;

public sealed class FieldSpec
{
    public string FieldKey { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string ExpectedTag { get; init; } = "input";
    public string ExpectedType { get; init; } = string.Empty;
    public IReadOnlyList<string> LabelAliases { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AttributeAliases { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DecoyTokens { get; init; } = Array.Empty<string>();
    public int OrderHint { get; init; } = -1;
}
