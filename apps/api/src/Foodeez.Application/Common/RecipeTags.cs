namespace Foodeez.Application.Common;

/// <summary>
/// Recipe tags are stored as one comma-separated string in a 500-character column, and every
/// place that writes them has to respect both facts.
/// </summary>
public static class RecipeTags
{
    /// <summary>The column's width. Tags are dropped whole rather than cut mid-word.</summary>
    private const int MaxLength = 500;

    /// <summary>
    /// Tags joined for storage, deduplicated case-insensitively and trimmed to fit.
    ///
    /// Whole tags come off the end when the string is too long: cutting at the character
    /// limit instead leaves a corrupted last tag behind, which then matches nothing and
    /// shows up as a nonsense pill.
    /// </summary>
    public static string? Join(IEnumerable<string> tags)
    {
        var kept = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            var trimmed = tag?.Trim();
            if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
            {
                kept.Add(trimmed);
            }
        }

        var joined = string.Join(",", kept);
        while (joined.Length > MaxLength && kept.Count > 0)
        {
            kept.RemoveAt(kept.Count - 1);
            joined = string.Join(",", kept);
        }

        return joined.Length == 0 ? null : joined;
    }

    /// <summary>The tags in a stored string, as the pills and prompts want them.</summary>
    public static List<string> Split(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
