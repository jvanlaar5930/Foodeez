using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.UseCases.MealPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/meal-plans")]
[Authorize]
public class MealPlansController : ControllerBase
{
    private readonly GetMealPlanUseCase _getMealPlanUseCase;
    private readonly CreateMealPlanUseCase _createMealPlanUseCase;
    private readonly GenerateAIMealPlanUseCase _generateAIMealPlanUseCase;

    public MealPlansController(
        GetMealPlanUseCase getMealPlanUseCase,
        CreateMealPlanUseCase createMealPlanUseCase,
        GenerateAIMealPlanUseCase generateAIMealPlanUseCase)
    {
        _getMealPlanUseCase = getMealPlanUseCase;
        _createMealPlanUseCase = createMealPlanUseCase;
        _generateAIMealPlanUseCase = generateAIMealPlanUseCase;
    }

    /// <summary>Get all meal plans for a user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MealPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMealPlans([FromQuery] Guid userId)
    {
        var plans = await _getMealPlanUseCase.ExecuteAsync(userId);
        return Ok(plans);
    }

    /// <summary>Create a new meal plan manually.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMealPlan([FromBody] CreateMealPlanRequest request)
    {
        var plan = await _createMealPlanUseCase.ExecuteAsync(request);
        return CreatedAtAction(nameof(GetMealPlans), new { userId = request.UserId }, plan);
    }

    /// <summary>Generate a meal plan using AI based on user profile and preferences.</summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GenerateMealPlan([FromBody] GenerateMealPlanRequest request, CancellationToken ct)
    {
        try
        {
            var plan = await _generateAIMealPlanUseCase.ExecuteAsync(request, ct);
            return CreatedAtAction(nameof(GetMealPlans), new { userId = request.UserId }, plan);
        }
        catch (AIGenerationFailedException ex)
        {
            // 503, not 500: the request was fine and retrying is the right response. Nothing
            // was saved, so there is no half-made plan for the client to reconcile.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }
}
