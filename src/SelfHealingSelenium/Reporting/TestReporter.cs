namespace SelfHealingSelenium.Reporting;

public static class TestReporter
{
    public static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    public static void PrintSummary(IReadOnlyList<TestResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("==================== TEST SUMMARY ====================");
        foreach (var result in results)
        {
            var status = result.Passed ? "PASS" : "FAIL";
            Console.WriteLine($"[{status}] {result.DisplayName} ({result.FieldKey}) - {result.Details} [{result.ElapsedMilliseconds} ms]");
            if (result.LocatorStrategy.Length > 0)
            {
                Console.WriteLine($"       locator: {result.LocatorStrategy} -> '{result.LocatorValue}'");
            }
        }

        var passed = results.Count(r => r.Passed);
        Console.WriteLine("======================================================");
        Console.WriteLine($"Total: {results.Count} | Passed: {passed} | Failed: {results.Count - passed}");
    }
}
