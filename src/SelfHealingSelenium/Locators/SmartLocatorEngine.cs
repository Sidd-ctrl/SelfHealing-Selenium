using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SelfHealingSelenium.Locators;

public sealed class SmartLocatorEngine
{
    private const double LabelExactWeight = 1.0;
    private const double LabelPartialWeight = 0.75;
    private const double AriaWeight = 0.95;
    private const double PlaceholderWeight = 0.8;
    private const double AttributeFuzzyWeight = 0.6;
    private const double NearbyTextWeight = 0.7;
    private const double TagTypeWeight = 0.35;
    private const double OrderBonus = 0.1;
    private const double DecoyPenalty = -0.5;

    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly double _confidenceThreshold;

    public SmartLocatorEngine(IWebDriver driver, int explicitWaitSeconds, double confidenceThreshold)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(explicitWaitSeconds));
        _confidenceThreshold = confidenceThreshold;
    }

    public LocatorCandidate FindCandidate(FieldSpec spec)
    {
        var candidates = CollectCandidates(spec);
        var best = SelectBestCandidate(spec, candidates);
        _wait.Until(_ => best.Element.Displayed);
        return best;
    }

    public IWebElement FindElement(FieldSpec spec)
    {
        return FindCandidate(spec).Element;
    }

    public IReadOnlyList<LocatorCandidate> CollectCandidates(FieldSpec spec)
    {
        var pool = new List<LocatorCandidate>();
        var elements = _driver.FindElements(By.CssSelector("input, select, textarea, [role='radio'], [role='combobox'], [role='textbox']"));

        foreach (var element in elements)
        {
            var candidate = ScoreElement(spec, element);
            if (candidate.Score > 0)
            {
                pool.Add(candidate);
            }
        }

        return pool
            .OrderByDescending(candidate => candidate.Score)
            .ToList();
    }

    private LocatorCandidate ScoreElement(FieldSpec spec, IWebElement element)
    {
        var candidate = new LocatorCandidate { Element = element, Score = 0 };
        var tag = SafeGet(element, "tagName").ToLowerInvariant();
        var type = SafeGet(element, "type");
        var id = SafeGet(element, "id");
        var name = SafeGet(element, "name");
        var placeholder = SafeGet(element, "placeholder");
        var ariaLabel = SafeGet(element, "aria-label");
        var ariaLabelledBy = SafeGet(element, "aria-labelledby");
        var title = SafeGet(element, "title");
        var role = SafeGet(element, "role");
        var cssClass = SafeGet(element, "class");

        var tagMatches = tag.Length > 0 && tag.Equals(spec.ExpectedTag, StringComparison.OrdinalIgnoreCase);
        var typeMatches = spec.ExpectedType.Length == 0 || type.Equals(spec.ExpectedType, StringComparison.OrdinalIgnoreCase);
        var roleMatches = spec.ExpectedTag == "input" && spec.ExpectedType == "radio" && role.Equals("radio", StringComparison.OrdinalIgnoreCase);

        if (!tagMatches && !roleMatches && !(spec.ExpectedTag == "select" && role.Equals("combobox", StringComparison.OrdinalIgnoreCase)))
        {
            if (!(tag == "select" && spec.ExpectedTag == "select"))
            {
                return candidate;
            }
        }

        if (spec.ExpectedTag == "input" && type.Length > 0 && spec.ExpectedType.Length > 0 && !typeMatches && role.Length == 0)
        {
            return candidate;
        }

        var labelText = GetAssociatedLabelText(element);
        var nearbyText = GetNearbyText(element);
        var labelledByText = ResolveAriaLabelledBy(ariaLabelledBy);
        var ancestorAria = GetAncestorAriaText(element);

        var labelSources = new[] { labelText, labelledByText, nearbyText }.Where(text => text.Length > 0).ToList();

        foreach (var source in labelSources)
        {
            var normalized = TextNormalizer.Normalize(source);
            if (spec.LabelAliases.Any(alias => TextNormalizer.Normalize(alias) == normalized))
            {
                candidate.Score = Math.Max(candidate.Score, LabelExactWeight);
                candidate.MatchedStrategy = "associated-label-exact";
                candidate.MatchedValue = source;
            }
            else if (TextNormalizer.ContainsAllTokens(source, spec.LabelAliases.SelectMany(a => TextNormalizer.Normalize(a).Split(' ')).Distinct()) && spec.LabelAliases.Any(alias => TextNormalizer.Normalize(alias).Length > 0))
            {
                candidate.Score = Math.Max(candidate.Score, LabelPartialWeight);
                candidate.MatchedStrategy = "associated-label-partial";
                candidate.MatchedValue = source;
            }
            else
            {
                var overlap = TextNormalizer.TokenOverlapScore(source, spec.LabelAliases);
                if (overlap >= 0.5)
                {
                    candidate.Score = Math.Max(candidate.Score, LabelPartialWeight * overlap + 0.15);
                    candidate.MatchedStrategy = "associated-label-fuzzy";
                    candidate.MatchedValue = source;
                }
            }
        }

        foreach (var aria in new[] { ariaLabel, labelledByText, ancestorAria })
        {
            if (aria.Length == 0)
            {
                continue;
            }

            if (spec.LabelAliases.Any(alias => TextNormalizer.Normalize(alias) == TextNormalizer.Normalize(aria)))
            {
                candidate.Score = Math.Max(candidate.Score, AriaWeight);
                candidate.MatchedStrategy = "aria-exact";
                candidate.MatchedValue = aria;
            }
            else
            {
                var overlap = TextNormalizer.TokenOverlapScore(aria, spec.LabelAliases);
                if (overlap >= 0.6)
                {
                    candidate.Score = Math.Max(candidate.Score, AriaWeight * overlap);
                    candidate.MatchedStrategy = "aria-fuzzy";
                    candidate.MatchedValue = aria;
                }
            }
        }

        if (placeholder.Length > 0)
        {
            if (spec.AttributeAliases.Any(alias => TextNormalizer.Normalize(alias) == TextNormalizer.Normalize(placeholder)))
            {
                candidate.Score = Math.Max(candidate.Score, PlaceholderWeight);
                candidate.MatchedStrategy = "placeholder-exact";
                candidate.MatchedValue = placeholder;
            }
            else
            {
                var overlap = TextNormalizer.TokenOverlapScore(placeholder, spec.AttributeAliases.Concat(spec.LabelAliases));
                if (overlap >= 0.6)
                {
                    candidate.Score = Math.Max(candidate.Score, PlaceholderWeight * overlap);
                    candidate.MatchedStrategy = "placeholder-fuzzy";
                    candidate.MatchedValue = placeholder;
                }
            }
        }

        foreach (var attribute in new[] { name, id, cssClass, title })
        {
            if (attribute.Length == 0)
            {
                continue;
            }

            var overlap = TextNormalizer.TokenOverlapScore(attribute, spec.AttributeAliases.Concat(spec.LabelAliases));
            if (overlap >= 0.6)
            {
                candidate.Score = Math.Max(candidate.Score, AttributeFuzzyWeight * overlap + 0.1);
                candidate.MatchedStrategy = "attribute-fuzzy";
                candidate.MatchedValue = attribute;
            }
        }

        if (candidate.Score <= 0 && tagMatches && typeMatches)
        {
            candidate.Score = TagTypeWeight;
            candidate.MatchedStrategy = "tag-type-heuristic";
            candidate.MatchedValue = $"{tag}[type={type}]";
        }

        var allVisibleText = string.Join(" ", labelText, ariaLabel, labelledByText, ancestorAria, placeholder, name, id, nearbyText);
        if (spec.DecoyTokens.Count > 0 && TextNormalizer.ContainsAnyToken(allVisibleText, spec.DecoyTokens))
        {
            candidate.Score += DecoyPenalty;
            candidate.MatchedStrategy += "|decoy-penalty";
        }

        if (spec.OrderHint >= 0)
        {
            candidate.Score += OrderBonus;
        }

        candidate.Reasoning = BuildReasoning(spec, candidate, tag, type, id, name, labelText, nearbyText);
        return candidate;
    }

    private LocatorCandidate SelectBestCandidate(FieldSpec spec, IReadOnlyList<LocatorCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            throw new LocatorAmbiguityException(spec.FieldKey, new[] { "no candidate elements found on the page" });
        }

        var best = candidates[0];
        var runnerUp = candidates.Count > 1 ? candidates[1] : null;

        if (best.Score < _confidenceThreshold)
        {
            throw new LocatorAmbiguityException(spec.FieldKey, DescribeTop(candidates, 5));
        }

        if (runnerUp != null && Math.Abs(best.Score - runnerUp.Score) < 0.05 && best.MatchedStrategy != runnerUp.MatchedStrategy)
        {
            throw new LocatorAmbiguityException(spec.FieldKey, DescribeTop(candidates, 5));
        }

        return best;
    }

    private static IReadOnlyList<string> DescribeTop(IReadOnlyList<LocatorCandidate> candidates, int count)
    {
        return candidates
            .Take(count)
            .Select(candidate => $"score={candidate.Score:F2} strategy={candidate.MatchedStrategy} value='{candidate.MatchedValue}'")
            .ToList();
    }

    private string GetAssociatedLabelText(IWebElement element)
    {
        var id = SafeGet(element, "id");
        if (id.Length > 0)
        {
            var byFor = _driver.FindElements(By.CssSelector($"label[for='{CssEscape(id)}']"));
            if (byFor.Count > 0)
            {
                return TextNormalizer.CollapseWhitespace(byFor[0].Text);
            }
        }

        var script = """
            const el = arguments[0];
            if (el.labels && el.labels.length > 0) {
                return Array.from(el.labels).map(l => l.innerText || l.textContent || '').join(' ').trim();
            }
            const parent = el.closest('label');
            if (parent) {
                return (parent.innerText || parent.textContent || '').trim();
            }
            return '';
            """;
        return ExecuteScript(script, element);
    }

    private string GetNearbyText(IWebElement element)
    {
        var script = """
            const el = arguments[0];
            const parts = [];
            let container = el.closest('div, fieldset, li, td, tr, section, form') || el.parentElement;
            if (container) {
                const label = container.querySelector('label, legend, .label, [class*="label"], h1, h2, h3, h4, span, p');
                if (label) parts.push(label.innerText || label.textContent || '');
            }
            let prev = el.previousElementSibling;
            let hops = 0;
            while (prev && hops < 3) {
                const text = (prev.innerText || prev.textContent || '').trim();
                if (text) parts.push(text);
                prev = prev.previousElementSibling;
                hops++;
            }
            return parts.join(' ').trim();
            """;
        return ExecuteScript(script, element);
    }

    private string GetAncestorAriaText(IWebElement element)
    {
        var script = """
            const el = arguments[0];
            let node = el.parentElement;
            while (node && node !== document.body && node !== document.documentElement) {
                const label = node.getAttribute('aria-label');
                if (label) return label.trim();
                const labelledBy = node.getAttribute('aria-labelledby');
                if (labelledBy) {
                    const text = labelledBy.split(/\\s+/).map(id => {
                        const ref = document.getElementById(id);
                        return ref ? (ref.innerText || ref.textContent || '') : '';
                    }).join(' ').trim();
                    if (text) return text;
                }
                node = node.parentElement;
            }
            return '';
            """;
        return ExecuteScript(script, element);
    }

    private string ResolveAriaLabelledBy(string ariaLabelledBy)
    {
        if (ariaLabelledBy.Length == 0)
        {
            return string.Empty;
        }

        var parts = ariaLabelledBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var texts = parts
            .Select(referenceId => _driver.FindElements(By.Id(referenceId)))
            .Where(matches => matches.Count > 0)
            .Select(matches => TextNormalizer.CollapseWhitespace(matches[0].Text));
        return TextNormalizer.CollapseWhitespace(string.Join(" ", texts));
    }

    private string ExecuteScript(string script, params object[] args)
    {
        var result = ((IJavaScriptExecutor)_driver).ExecuteScript(script, args);
        return result as string ?? string.Empty;
    }

    private static string SafeGet(IWebElement element, string attribute)
    {
        try
        {
            return element.GetAttribute(attribute) ?? string.Empty;
        }
        catch (WebDriverException)
        {
            return string.Empty;
        }
    }

    private static string CssEscape(string value)
    {
        return value.Replace("'", "\\'");
    }

    private static string BuildReasoning(FieldSpec spec, LocatorCandidate candidate, string tag, string type, string id, string name, string labelText, string nearbyText)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append($"field={spec.FieldKey} ");
        builder.Append($"tag={tag} type={type} id='{id}' name='{name}' ");
        builder.Append($"label='{labelText}' nearby='{nearbyText}' ");
        builder.Append($"strategy={candidate.MatchedStrategy} score={candidate.Score:F2}");
        return builder.ToString();
    }
}
