using System.Text;
using System.Text.RegularExpressions;

namespace SelfHealingSelenium.Locators;

public static class TextNormalizer
{
    private static readonly Regex NonAlphaNumeric = new("[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new("\\s+", RegexOptions.Compiled);

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lowered = value.Trim().ToLowerInvariant();
        return NonAlphaNumeric.Replace(lowered, " ").Trim();
    }

    public static string CollapseWhitespace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : Whitespace.Replace(value.Trim(), " ");
    }

    public static bool ContainsAllTokens(string? haystack, IEnumerable<string> tokens)
    {
        var normalized = Normalize(haystack);
        return tokens.All(token => normalized.Contains(Normalize(token), StringComparison.Ordinal));
    }

    public static bool ContainsAnyToken(string? haystack, IEnumerable<string> tokens)
    {
        var normalized = Normalize(haystack);
        return tokens.Any(token => normalized.Contains(Normalize(token), StringComparison.Ordinal));
    }

    public static double TokenOverlapScore(string? value, IEnumerable<string> aliases)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            return 0;
        }

        var aliasTokens = aliases
            .SelectMany(alias => Normalize(alias).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();

        if (aliasTokens.Count == 0)
        {
            return 0;
        }

        var valueTokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matched = aliasTokens.Count(token => valueTokens.Contains(token, StringComparer.Ordinal));
        return (double)matched / aliasTokens.Count;
    }

    public static string ToSnakeCase(string value)
    {
        var normalized = Normalize(value);
        return normalized.Replace(' ', '-');
    }
}
