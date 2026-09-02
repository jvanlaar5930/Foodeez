using Foodeez.API.Streaming;
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
    private readonly EstimateNutritionUseCase _estimateNutritionUseCase;
    private readonly StreamMealAnalysisUseCase _streamMealAnalysisUseCase;

    public AIController(
        GetDietaryRecommendationsUseCase getRecommendationsUseCase,
        AnalyzeMealUseCase analyzeMealUseCase,
        EstimateNutritionUseCase estimateNutritionUseCase,
        StreamMealAnalysisUseCase streamMealAnalysisUseCase)
    {
        _getRecommendationsUseCase = getRecommendationsUseCase;
        _analyzeMealUseCase = analyzeMealUseCase;
        _estimateNutritionUseCase = estimateNutritionUseCase;
        _streamMealAnalysisUseCase = streamMealAnalysisUseCase;
    }

    // MVC binds the CancellationToken parameters below to HttpContext.RequestAborted, so a
    // client that navigates away, hits Escape, or times out actually stops the upstream
    // model call rather than leaving it running to completion for nobody.

    /// <summary>Get AI-powered dietary recommendations based on user profile and recent nutrition history.</summary>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(DietaryRecommendationsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommendations([FromBody] GetRecommendationsRequest request, CancellationToken ct)
    {
        var recommendations = await _getRecommendationsUseCase.ExecuteAsync(request.UserId, ct);
        return Ok(recommendations);
    }

    /// <summary>Analyze a meal for nutritional completeness and get suggestions to improve it.</summary>
    [HttpPost("analyze-meal")]
    [ProducesResponseType(typeof(MealAnalysisDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeMeal([FromBody] MealAnalysisRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return BadRequest("At least one food item is required.");

        var result = await _analyzeMealUseCase.ExecuteAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// The same analysis as analyze-meal, streamed as server-sent events: "delta" events carry
    /// the assessment as the model writes it, and a final "result" event carries the parsed
    /// analysis (or "error" if none could be produced).
    /// </summary>
    [HttpPost("analyze-meal/stream")]
    [Produces("text/event-stream")]
    public async Task AnalyzeMealStream([FromBody] MealAnalysisRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await ServerSentEventStream.WriteAsync(Response, _streamMealAnalysisUseCase.ExecuteAsync(request, ct), ct);
    }

    /// <summary>
    /// Estimate per-serving nutrition for a home-cooked dish. Always returns 200: a failed
    /// estimate comes back with Succeeded=false so the client can fall back to manual entry
    /// rather than treating it as an error.
    /// </summary>
    [HttpPost("estimate-nutrition")]
    [ProducesResponseType(typeof(EstimatedNutritionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EstimateNutrition([FromBody] EstimateNutritionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("A dish name is required.");

        return Ok(await _estimateNutritionUseCase.ExecuteAsync(request, ct));
    }
}

public class GetRecommendationsRequest
{
    public Guid UserId { get; set; }
}
