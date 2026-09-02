using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.MealLogs;

/// <summary>
/// The photo route into the same place quick add lands: a plate becomes a list of foods with
/// amounts, matched against the food database exactly as a described meal is.
/// </summary>
public class ParseMealImageUseCase
{
    private readonly IAIService _aiService;
    private readonly ParsedMealResolver _resolver;

    public ParseMealImageUseCase(IAIService aiService, ParsedMealResolver resolver)
    {
        _aiService = aiService;
        _resolver = resolver;
    }

    public async Task<QuickAddResultDto> ExecuteAsync(Guid userId, byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
    {
        var parsed = await _aiService.ParseMealImageAsync(imageData, mimeType, ct);
        var result = await _resolver.ResolveAsync(userId, parsed, ct);

        if (result.Items.Count == 0)
        {
            result.Note ??= "No food could be picked out of that photo. Try a clearer shot, or describe the meal instead.";
        }

        return result;
    }
}
