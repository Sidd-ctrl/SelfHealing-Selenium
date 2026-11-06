using SelfHealingSelenium.Locators;

namespace SelfHealingSelenium.Automation;

public static class FieldSpecs
{
    public static readonly FieldSpec FirstName = new()
    {
        FieldKey = "firstName",
        DisplayName = "First Name text field",
        ExpectedTag = "input",
        ExpectedType = "text",
        LabelAliases = new[] { "first name", "firstname", "given name", "first" },
        AttributeAliases = new[] { "first name", "firstname", "fname", "given name" },
        DecoyTokens = new[] { "last", "middle", "surname", "family", "email", "phone" },
        OrderHint = 0
    };

    public static readonly FieldSpec Gender = new()
    {
        FieldKey = "gender",
        DisplayName = "Gender radio button",
        ExpectedTag = "input",
        ExpectedType = "radio",
        LabelAliases = new[] { "gender", "sex", "male", "female", "other" },
        AttributeAliases = new[] { "gender", "sex" },
        DecoyTokens = new[] { "marital", "title", "suffix" },
        OrderHint = 1
    };

    public static readonly FieldSpec State = new()
    {
        FieldKey = "state",
        DisplayName = "State dropdown",
        ExpectedTag = "select",
        ExpectedType = string.Empty,
        LabelAliases = new[] { "state", "province", "region", "select state" },
        AttributeAliases = new[] { "state", "province", "region" },
        DecoyTokens = new[] { "country", "city", "zip", "postal" },
        OrderHint = 2
    };

    public static IReadOnlyList<FieldSpec> All => new[] { FirstName, Gender, State };
}
