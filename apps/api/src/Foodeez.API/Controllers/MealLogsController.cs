using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.UseCases.MealLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/meal-logs")]
[Authorize]
public class MealLogsController : ControllerBase
{
    private readonly LogMealUseCase _logMealUseCase;
    private readonly ParseFoodImageUseCase _parseFoodImageUseCase;
    private readonly GetDailyLogsUseCase _getDailyLogsUseCase;
    private readonly GetNutritionSummaryUseCase _getNutritionSummaryUseCase;

    public MealLogsController(
        LogMealUseCase logMealUseCase,
        ParseFoodImageUseCase parseFoodImageUseCase,
        GetDailyLogsUseCase getDailyLogsUseCase,
        GetNutritionSummaryUseCase getNutritionSummaryUseCase)
    {
        _logMealUseCase = logMealUseCase;
        _parseFoodImageUseCase = parseFoodImageUseCase;
        _getDailyLogsUseCase = getDailyLogsUseCase;
        _getNutritionSummaryUseCase = getNutritionSummaryUseCase;
    }

    /// <summary>Log a meal with food items and quantities.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MealLogDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogMeal([FromBody] LogMealRequest request)
    {
        var result = await _logMealUseCase.ExecuteAsync(request);
        return CreatedAtAction(nameof(LogMeal), new { id = result.Id }, result);
    }

    /// <summary>Parse a food image using AI to extract nutritional information.</summary>
    [HttpPost("parse-image")]
    [ProducesResponseType(typeof(ParsedFoodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ParseFoodImage(IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest("No image file provided.");

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        var imageData = ms.ToArray();
        var mimeType = image.ContentType;

        var result = await _parseFoodImageUseCase.ExecuteAsync(imageData, mimeType);
        return Ok(result);
    }

    /// <summary>Get all meal logs for a user on a specific date.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MealLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyLogs([FromQuery] Guid userId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        var logs = await _getDailyLogsUseCase.ExecuteAsync(userId, parsedDate);
        return Ok(logs);
    }

    /// <summary>Get nutritional summary for a user on a specific date, including progress toward targets.</summary>
    [HttpGet("nutrition-summary")]
    [ProducesResponseType(typeof(NutritionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNutritionSummary([FromQuery] Guid userId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        var summary = await _getNutritionSummaryUseCase.ExecuteAsync(userId, parsedDate);
        return Ok(summary);
    }
}
