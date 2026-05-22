using Foodeez.Application.DTOs.AI;
using Foodeez.Application.UseCases.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly GetDietaryRecommendationsUseCase _getRecommendationsUseCase;
    private readonly AnalyzeMealUseCase _analyzeMealUseCase;

    public AIController(
        GetDietaryRecommendationsUseCase getRecommendationsUseCase,
        AnalyzeMealUseCase analyzeMealUseCase)
    {
        _getRecommendationsUseCase = getRecommendationsUseCase;
        _analyzeMealUseCase = analyzeMealUseCase;
    }

    /// <summary>Get AI-powered dietary recommendations based on user profile and recent nutrition history.</summary>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(DietaryRecommendationsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommendations([FromBody] GetRecommendationsRequest request)
    {
        var recommendations = await _getRecommendationsUseCase.ExecuteAsync(request.UserId);
        return Ok(recommendations);
    }

    /// <summary>Analyze a meal for nutritional completeness and get suggestions to improve it.</summary>
    [HttpPost("analyze-meal")]
    [ProducesResponseType(typeof(MealAnalysisDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeMeal([FromBody] MealAnalysisRequest request)
    {
        if (request.Items.Count == 0)
            return BadRequest("At least one food item is required.");

        var result = await _analyzeMealUseCase.ExecuteAsync(request);
        return Ok(result);
    }
}

public class GetRecommendationsRequest
{
    public Guid UserId { get; set; }
}
