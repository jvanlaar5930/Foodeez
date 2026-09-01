namespace Foodeez.Domain.ValueObjects;

/// <summary>
/// An AI verdict on a logged meal, stored with the meal so the model is asked once.
/// <see cref="Fingerprint"/> records the items the verdict was formed from: while it still
/// matches the meal, the stored analysis is served as-is, and only edits to the meal (or an
/// explicit refresh) justify paying for a new one.
/// </summary>
public class MealAnalysis
{
    public int Score { get; private set; }
    public string Completeness { get; private set; } = string.Empty;
    public List<string> Missing { get; private set; } = new();
    public List<string> Suggestions { get; private set; } = new();
    public string Fingerprint { get; private set; } = string.Empty;
    public DateTime GeneratedAt { get; private set; }

    // Required by EF Core
    private MealAnalysis() { }

    public MealAnalysis(
        int score,
        string completeness,
        IEnumerable<string> missing,
        IEnumerable<string> suggestions,
        string fingerprint,
        DateTime generatedAt)
    {
        Score = score;
        Completeness = completeness;
        Missing = missing.ToList();
        Suggestions = suggestions.ToList();
        Fingerprint = fingerprint;
        GeneratedAt = generatedAt;
    }

    /// <summary>True when this analysis still describes the meal identified by <paramref name="fingerprint"/>.</summary>
    public bool Matches(string fingerprint) => Fingerprint == fingerprint;
}
