using Foodeez.Domain.Common;

namespace Foodeez.Domain.Entities;

/// <summary>
/// The AI's read on one user's whole day of eating, kept so a day is analysed once.
/// <see cref="Fingerprint"/> records the meals and targets it was formed from: while it still
/// matches, the stored analysis is served as-is, and only a change to the day (or an explicit
/// refresh) justifies asking the model again.
/// </summary>
public class DayAnalysis : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }

    public int Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Gaps { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    public string Fingerprint { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    public User User { get; set; } = null!;

    /// <summary>True when this analysis still describes the day identified by <paramref name="fingerprint"/>.</summary>
    public bool Matches(string fingerprint) => Fingerprint == fingerprint;
}
