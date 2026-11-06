# SelfHealingSelenium

Self-healing Selenium WebDriver automation for the CloudQA Automation Practice Form. The suite tests three fields — First Name, Gender radio button, and State dropdown — using a reusable smart locator engine instead of fixed selectors.

## Technology stack

- C# / .NET 10
- Selenium WebDriver 4.25 (+ Selenium.Support for `SelectElement` and wait helpers)
- Microsoft.Extensions.Configuration (JSON, environment variables, command line)

## Prerequisites

- .NET 10 SDK installed (any newer SDK also works)
- Google Chrome or Microsoft Edge installed (matching WebDriver is fetched automatically by Selenium 4.25's built-in driver manager)
- VS Code with the C# Dev Kit extension (optional, for debugging)

## Installation

```powershell
dotnet restore src/SelfHealingSelenium/SelfHealingSelenium.csproj
```

## Configuration

All settings live in `src/SelfHealingSelenium/appsettings.json` and can be overridden by environment variables or command-line arguments (command line wins):

| Key | Meaning | Default |
| --- | --- | --- |
| `TargetUrl` | Page under test | `https://app.cloudqa.io/home/AutomationPracticeForm` |
| `Browser` | `Chrome` or `Edge` | `Chrome` |
| `BrowserPath` | Explicit browser executable path (leave empty to auto-detect) | empty |
| `Headless` | Run browser headless | `true` |
| `ExplicitWaitSeconds` | Explicit wait timeout | `15` |
| `ConfidenceThreshold` | Minimum locator confidence | `0.55` |
| `MaxRetryAttempts` | Retries per field | `3` |
| `FixturesDirectory` | Local HTML fixtures folder | `Fixtures` |

See `.env.example` for optional environment overrides. No API keys are required for this project.

## How to run in VS Code

Open the folder `SelfHealingSelenium` in VS Code, then in the integrated terminal:

Run against the live CloudQA page:

```powershell
dotnet run --project src/SelfHealingSelenium -- --TargetUrl "https://app.cloudqa.io/home/AutomationPracticeForm"
```

Run against a local mutated fixture (demonstrates resilience):

```powershell
dotnet run --project src/SelfHealingSelenium -- --TargetUrl "file:///C:/path/to/SelfHealingSelenium/src/SelfHealingSelenium/Fixtures/variant-mutated.html"
dotnet run --project src/SelfHealingSelenium -- --TargetUrl "file:///C:/path/to/SelfHealingSelenium/src/SelfHealingSelenium/Fixtures/variant-heavily-mutated.html"
```

Build without running:

```powershell
dotnet build SelfHealingSelenium.sln
```

Exit code is `0` when all three fields PASS, `1` on any FAIL, `2` on configuration error.

## How the smart locator engine works

For each field, a `FieldSpec` declares label aliases, attribute aliases, expected tag/type, decoy tokens, and an order hint. The engine (`SmartLocatorEngine`) evaluates every interactive element on the page against multiple independent strategies:

1. Associated `<label for>` and wrapping `<label>` text (exact and fuzzy)
2. `aria-label` / `aria-labelledby` (on the element and on ancestors)
3. `placeholder` text
4. Fuzzy matching of `name`, `id`, `class`, `title` attributes
5. Nearby text: sibling/parent container labels, legends, headings (including table rows)
6. Tag + input-type heuristic fallback

Each candidate receives a weighted score; decoy tokens (e.g. "last", "middle", "country") apply a penalty. The best candidate is accepted only above the confidence threshold and when no runner-up is nearly tied via a different strategy; otherwise a `LocatorAmbiguityException` is raised and the field is retried. Every interaction is verified afterwards (value read-back, radio `Selected` state, dropdown selected option) inside explicit waits.

## Local test fixtures

`src/SelfHealingSelenium/Fixtures/` contains three variants of the practice form:

- `variant-original.html` — baseline structure
- `variant-mutated.html` — changed IDs/names, extra wrappers, decoy fields (middle/last name), ancestor `aria-label`, `aria-labelledby` on the dropdown, reordered sections
- `variant-heavily-mutated.html` — table layout, reordered fields (state before name), generic attribute names, country dropdown decoy

## Known limitations

- The live CloudQA page must be reachable from your machine; if it is unavailable, run against the local fixtures instead (results against the live site are not fabricated here).
- The engine targets the documented field set (first name, gender, state); new fields require a new `FieldSpec`.
- Scoring weights are tuned heuristics, not machine-learned.
