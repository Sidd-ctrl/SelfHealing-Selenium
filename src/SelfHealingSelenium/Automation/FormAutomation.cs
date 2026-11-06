using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SelfHealingSelenium.Locators;
using SelfHealingSelenium.Reporting;

namespace SelfHealingSelenium.Automation;

public sealed class FormAutomation
{
    private const string FirstNameValue = "Aarav";
    private const string GenderValue = "Male";
    private const string StateValue = "California";

    private readonly IWebDriver _driver;
    private readonly SmartLocatorEngine _locatorEngine;
    private readonly WebDriverWait _wait;
    private readonly int _maxRetryAttempts;

    public FormAutomation(IWebDriver driver, SmartLocatorEngine locatorEngine, int explicitWaitSeconds, int maxRetryAttempts)
    {
        _driver = driver;
        _locatorEngine = locatorEngine;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(explicitWaitSeconds));
        _maxRetryAttempts = maxRetryAttempts;
    }

    public TestResult TestFirstName()
    {
        return RunWithRetry(FieldSpecs.FirstName, element =>
        {
            _wait.Until(d => element.Displayed && element.Enabled);
            element.Clear();
            element.SendKeys(FirstNameValue);
            var actual = element.GetAttribute("value");
            if (actual != FirstNameValue)
            {
                throw new InvalidOperationException($"First name verification failed. Expected '{FirstNameValue}' but field holds '{actual}'.");
            }
            return $"value verified as '{actual}'";
        });
    }

    public TestResult TestGender()
    {
        return RunWithRetry(FieldSpecs.Gender, _ =>
        {
            var radioGroup = _locatorEngine.CollectCandidates(FieldSpecs.Gender);
            var target = radioGroup
                .Select(candidate => candidate.Element)
                .FirstOrDefault(element =>
                {
                    var value = SafeAttribute(element, "value");
                    var label = GetLabelFor(element);
                    return value.Equals(GenderValue, StringComparison.OrdinalIgnoreCase)
                        || label.Equals(GenderValue, StringComparison.OrdinalIgnoreCase)
                        || label.Contains(GenderValue, StringComparison.OrdinalIgnoreCase);
                });

            if (target == null)
            {
                var descriptions = radioGroup.Select(c => $"value='{SafeAttribute(c.Element, "value")}' label='{GetLabelFor(c.Element)}'").ToList();
                throw new LocatorAmbiguityException(FieldSpecs.Gender.FieldKey, descriptions);
            }

            _wait.Until(d => target.Displayed && target.Enabled);
            target.Click();
            _wait.Until(d => target.Selected);
            if (!target.Selected)
            {
                throw new InvalidOperationException("Gender radio was clicked but is not selected.");
            }

            return $"radio '{GenderValue}' verified selected";
        });
    }

    public TestResult TestState()
    {
        return RunWithRetry(FieldSpecs.State, element =>
        {
            _wait.Until(d => element.Displayed && element.Enabled);
            var select = new SelectElement(element);
            select.SelectByText(StateValue);
            var selected = select.SelectedOption.Text;
            if (!selected.Equals(StateValue, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"State verification failed. Expected '{StateValue}' but selected '{selected}'.");
            }
            return $"dropdown verified with selected option '{selected}'";
        });
    }

    private TestResult RunWithRetry(FieldSpec spec, Func<IWebElement, string> interaction)
    {
        var result = new TestResult { FieldKey = spec.FieldKey, DisplayName = spec.DisplayName };
        var stopwatch = Stopwatch.StartNew();
        Exception? lastError = null;

        for (var attempt = 1; attempt <= _maxRetryAttempts; attempt++)
        {
            try
            {
                TestReporter.Log($"[{spec.FieldKey}] attempt {attempt}/{_maxRetryAttempts}: locating field via smart locator engine");
                var best = _locatorEngine.FindCandidate(spec);
                var element = best.Element;
                result.LocatorStrategy = best.MatchedStrategy;
                result.LocatorValue = best.MatchedValue;
                TestReporter.Log($"[{spec.FieldKey}] located via '{result.LocatorStrategy}' matching '{result.LocatorValue}'");
                result.Details = interaction(element);
                result.Passed = true;
                TestReporter.Log($"[{spec.FieldKey}] PASS - {result.Details}");
                break;
            }
            catch (Exception error) when (attempt < _maxRetryAttempts)
            {
                lastError = error;
                TestReporter.Log($"[{spec.FieldKey}] attempt {attempt} failed: {error.Message}");
            }
            catch (Exception error)
            {
                lastError = error;
                result.Details = error.Message;
                TestReporter.Log($"[{spec.FieldKey}] FAIL - {error.Message}");
            }
        }

        stopwatch.Stop();
        result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        if (!result.Passed && result.Details.Length == 0)
        {
            result.Details = lastError?.Message ?? "unknown failure";
        }

        return result;
    }

    private static string SafeAttribute(IWebElement element, string attribute)
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

    private string GetLabelFor(IWebElement element)
    {
        var id = SafeAttribute(element, "id");
        if (id.Length > 0)
        {
            var labels = _driver.FindElements(By.CssSelector($"label[for='{id}']"));
            if (labels.Count > 0)
            {
                return labels[0].Text;
            }
        }

        var script = """
            const el = arguments[0];
            if (el.labels && el.labels.length > 0) return Array.from(el.labels).map(l => l.innerText || '').join(' ').trim();
            const parent = el.closest('label');
            return parent ? (parent.innerText || '').trim() : '';
            """;
        return ((IJavaScriptExecutor)_driver).ExecuteScript(script, element) as string ?? string.Empty;
    }
}
