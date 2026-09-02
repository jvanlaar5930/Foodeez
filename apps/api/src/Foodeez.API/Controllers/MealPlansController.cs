using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Foodeez.API.Streaming;
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
    private readonly SaveMealPlanEntryUseCase _saveEntryUseCase;

    public MealPlansController(
        GetMealPlanUseCase getMealPlanUseCase,
        CreateMealPlanUseCase createMealPlanUseCase,
        GenerateAIMealPlanUseCase generateAIMealPlanUseCase,
        SaveMealPlanEntryUseCase saveEntryUseCase)
    {
        _getMealPlanUseCase = getMealPlanUseCase;
        _createMealPlanUseCase = createMealPlanUseCase;
        _generateAIMealPlanUseCase = generateAIMealPlanUseCase;
        _saveEntryUseCase = saveEntryUseCase;
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

    /// <summary>
    /// The same generation, streamed as server-sent events: "delta" events carry the plan's
    /// rationale as the model writes it, and a final "result" event carries the saved plan.
    /// </summary>
    [HttpPost("generate/stream")]
    [Produces("text/event-stream")]
    public async Task GenerateMealPlanStream([FromBody] GenerateMealPlanRequest request, CancellationToken ct)
    {
        await ServerSentEventStream.WriteAsync(
            Response, _generateAIMealPlanUseCase.ExecuteStreamAsync(request, ct), ct);
    }

    /// <summary>Put a meal into one slot of a plan, replacing whatever was in it.</summary>
    [HttpPost("{planId:guid}/entries")]
    [ProducesResponseType(typeof(MealPlanEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddEntry(
        [FromRoute] Guid planId, [FromBody] MealPlanEntryRequest request, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId) return Unauthorized();

        var result = await _saveEntryUseCase.AddAsync(planId, userId, request, ct);
        return result.Outcome switch
        {
            SaveEntryOutcome.Saved => CreatedAtAction(
                nameof(GetMealPlans), new { userId }, result.Entry),
            SaveEntryOutcome.Rejected => BadRequest(new { message = result.Reason }),
            _ => NotFound()
        };
    }

    /// <summary>Change the meal in one slot, including moving it to another day or slot.</summary>
    [HttpPut("{planId:guid}/entries/{entryId:guid}")]
    [ProducesResponseType(typeof(MealPlanEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntry(
        [FromRoute] Guid planId,
        [FromRoute] Guid entryId,
        [FromBody] MealPlanEntryRequest request,
        CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId) return Unauthorized();

        var result = await _saveEntryUseCase.UpdateAsync(planId, entryId, userId, request, ct);
        return result.Outcome switch
        {
            SaveEntryOutcome.Saved => Ok(result.Entry),
            SaveEntryOutcome.Rejected => BadRequest(new { message = result.Reason }),
            _ => NotFound()
        };
    }

    /// <summary>Clear one slot.</summary>
    [HttpDelete("{planId:guid}/entries/{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(
        [FromRoute] Guid planId, [FromRoute] Guid entryId, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId) return Unauthorized();

        var result = await _saveEntryUseCase.DeleteAsync(planId, entryId, userId, ct);
        return result.Outcome == SaveEntryOutcome.Saved ? NoContent() : NotFound();
    }

    /// <summary>The authenticated user's id, from the token's subject claim.</summary>
    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
