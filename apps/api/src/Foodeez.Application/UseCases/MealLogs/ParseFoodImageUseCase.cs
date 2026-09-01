using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.MealLogs;

public class ParseFoodImageUseCase
{
    private readonly IAIService _aiService;

    public ParseFoodImageUseCase(IAIService aiService)
    {
        _aiService = aiService;
    }

    public async Task<ParsedFoodDto> ExecuteAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
    {
        return await _aiService.ParseFoodImageAsync(imageData, mimeType, ct);
    }
}
