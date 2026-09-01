using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foodeez.Infrastructure.Services.Json;

/// <summary>
/// Spoonacular sends <c>null</c> for numeric fields it has no value for - most recipes carry
/// <c>"preparationMinutes": null</c> - and occasionally sends a number as a JSON string.
/// System.Text.Json throws on null-to-struct by default, and because a single bad property
/// fails the whole response, one null minute count silently emptied every search result.
/// These converters coerce null and string forms to a sane default instead.
/// </summary>
public sealed class NullTolerantInt32Converter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return 0;
            case JsonTokenType.String:
                return int.TryParse(reader.GetString(), out var parsed) ? parsed : 0;
            default:
                if (reader.TryGetInt32(out var value)) return value;
                // Spoonacular sometimes reports whole numbers as decimals (e.g. 45.0).
                return reader.TryGetDouble(out var d) ? (int)Math.Round(d) : 0;
        }
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

/// <inheritdoc cref="NullTolerantInt32Converter"/>
public sealed class NullTolerantDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return 0d;
            case JsonTokenType.String:
                return double.TryParse(reader.GetString(), out var parsed) ? parsed : 0d;
            default:
                return reader.TryGetDouble(out var value) ? value : 0d;
        }
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

/// <inheritdoc cref="NullTolerantInt32Converter"/>
public sealed class NullTolerantBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.Null => false,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => bool.TryParse(reader.GetString(), out var parsed) && parsed,
            _ => false
        };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}
