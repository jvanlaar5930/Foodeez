using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Foodeez.Infrastructure.Data.Configurations;

/// <summary>
/// Stores a short list of strings - the phrases the AI hands back - in one text column, which
/// is all these lists are ever read as: whole, with the row that owns them.
/// </summary>
internal static class JsonStringList
{
    public static readonly ValueConverter<List<string>, string> Converter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

    public static readonly ValueComparer<List<string>> Comparer = new(
        (a, b) => a!.SequenceEqual(b!),
        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
        v => v.ToList());
}
