namespace SelfHealingSelenium.Locators;

public sealed class LocatorAmbiguityException : Exception
{
    public IReadOnlyList<string> CandidateDescriptions { get; }

    public LocatorAmbiguityException(string fieldKey, IReadOnlyList<string> candidateDescriptions)
        : base($"Could not confidently locate field '{fieldKey}'. Candidates considered: {string.Join(" | ", candidateDescriptions)}")
    {
        CandidateDescriptions = candidateDescriptions;
    }
}
