using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Serializer settings for talking to AI providers. Every provider declared its own private,
/// identical copy of this; sharing one instance also avoids re-creating the options object,
/// which is expensive enough that System.Text.Json documents caching it.
/// </summary>
internal static class AIJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
