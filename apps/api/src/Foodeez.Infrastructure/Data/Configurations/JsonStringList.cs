using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Foodeez.Infrastructure.Data.Configurations;

/// <summary>
/// Stores a short list of strings - the phrases the AI hands back, the foods someone avoids -
/// in one text column, which is all these lists are ever read as: whole, with the row that
/// owns them.
/// </summary>
internal static class JsonStringList
{
    public static readonly ValueConverter<List<string>, string> Converter = new(
        list => Write(list),
        text => Read(text));

    public static readonly ValueComparer<List<string>> Comparer = new(
        (a, b) => a!.SequenceEqual(b!),
        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
        v => v.ToList());

    private static string Write(List<string> list) =>
        JsonSerializer.Serialize(list, (JsonSerializerOptions?)null);

    /// <summary>
    /// Anything that is not a JSON array reads as an empty list. A column added to a table
    /// that already has rows starts life as an empty string, not as "[]", and a row written
    /// before this was JSON is not worth an exception on every load.
    /// </summary>
    private static List<string> Read(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(text, (JsonSerializerOptions?)null) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
