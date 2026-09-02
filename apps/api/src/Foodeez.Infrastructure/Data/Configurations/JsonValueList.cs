using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Foodeez.Infrastructure.Data.Configurations;

/// <summary>
/// The <see cref="JsonStringList"/> treatment for a list of small objects: written whole,
/// read whole, never queried into. Same tolerance for a column that predates the JSON, since
/// a list of suggestions is not worth an exception on every load of a conversation.
/// </summary>
internal static class JsonValueList<T>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly ValueConverter<List<T>, string> Converter = new(
        list => JsonSerializer.Serialize(list, Options),
        text => Read(text));

    public static readonly ValueComparer<List<T>> Comparer = new(
        (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
        v => JsonSerializer.Serialize(v, Options).GetHashCode(),
        v => JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(v, Options), Options) ?? new List<T>());

    private static List<T> Read(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<T>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(text, Options) ?? new List<T>();
        }
        catch (JsonException)
        {
            return new List<T>();
        }
    }
}
