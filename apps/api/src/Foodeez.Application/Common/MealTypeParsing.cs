using System.Text.Json;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.Common;

/// <summary>
/// Reads the meal slot out of an AI response.
///
/// Models answer this field however they feel like - 3, "3", "lunch", "Lunch",
/// "afternoon_snack" - and the previous <c>(MealType)element.GetInt32()</c> threw on every
/// one of the string forms. That exception was caught by the parser's outer handler, which
/// discarded the whole plan, so a model with a different formatting habit produced a
/// silently empty week rather than a visible error.
/// </summary>
public static class MealTypeParsing
{
    public static MealType Read(JsonElement element, MealType fallback = MealType.Breakfast)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out var n) && IsDefined(n) ? (MealType)n : fallback;

        if (element.ValueKind != JsonValueKind.String)
            return fallback;

        var raw = element.GetString();
        if (string.IsNullOrWhiteSpace(raw)) return fallback;

        // A numeric string is still a number.
        if (int.TryParse(raw, out var parsed) && IsDefined(parsed))
            return (MealType)parsed;

        // Strip the separators the various wordings use, so "afternoon_snack",
        // "afternoon-snack" and "Afternoon Snack" all land on the same member.
        var normalized = new string(raw.Where(char.IsLetterOrDigit).ToArray());

        foreach (var value in Enum.GetValues<MealType>())
        {
            if (string.Equals(value.ToString(), normalized, StringComparison.OrdinalIgnoreCase))
                return value;
        }

        // The short labels the calendar itself shows. A model that has seen them - or that
        // simply reaches for the common wording - would otherwise fall through to the fallback
        // and be filed under whatever that happens to be, which is silent and wrong.
        var alias = normalized.ToLowerInvariant() switch
        {
            "amsnack" => MealType.MorningSnack,
            "pmsnack" => MealType.AfternoonSnack,
            "evening" => MealType.EveningSnack,
            // "snack" on its own is common and has no exact member.
            "snack" => MealType.AfternoonSnack,
            _ => (MealType?)null
        };

        return alias ?? fallback;
    }

    private static bool IsDefined(int value) => Enum.IsDefined(typeof(MealType), value);
}
