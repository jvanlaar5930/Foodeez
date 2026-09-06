using Foodeez.API.Streaming;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.UseCases.MealPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[Route("api/meal-plans")]
[Authorize]
public class MealPlansController : FoodeezController
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

    /// <summary>Get all meal plans for the signed-in user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MealPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMealPlans()
    {
        var plans = await _getMealPlanUseCase.ExecuteAsync(UserId);
        return Ok(plans);
    }

    /// <summary>Create a new meal plan manually.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMealPlan([FromBody] CreateMealPlanRequest request)
    {
        var userId = UserId;

        // The body carries a userId for symmetry with the rest of this controller, but the
        // plan is filed against the token's user, never the body's.
        request.UserId = userId;

        var plan = await _createMealPlanUseCase.ExecuteAsync(request);
        return CreatedAtAction(nameof(GetMealPlans), null, plan);
    }

    /// <summary>Generate a meal plan using AI based on user profile and preferences.</summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GenerateMealPlan([FromBody] GenerateMealPlanRequest request, CancellationToken ct)
    {
        request.UserId = UserId;

        // AIGenerationFailedException is mapped to 503 by ExceptionHandlingMiddleware.
        var plan = await _generateAIMealPlanUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetMealPlans), null, plan);
    }

    /// <summary>
    /// The same generation, streamed as server-sent events: "delta" events carry the plan's
    /// rationale as the model writes it, and a final "result" event carries the saved plan.
    /// </summary>
    [HttpPost("generate/stream")]
    [Produces("text/event-stream")]
    public async Task GenerateMealPlanStream([FromBody] GenerateMealPlanRequest request, CancellationToken ct)
    {
        request.UserId = UserId;

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
        var userId = UserId;

        var result = await _saveEntryUseCase.AddAsync(planId, userId, request, ct);
        return result.Outcome switch
        {
            SaveEntryOutcome.Saved => CreatedAtAction(
                nameof(GetMealPlans), new { userId }, result.Entry),
            SaveEntryOutcome.Rejected => BadRequest(Failure(result.Reason ?? "That change was rejected.")),
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
        var result = await _saveEntryUseCase.UpdateAsync(planId, entryId, UserId, request, ct);
        return result.Outcome switch
        {
            SaveEntryOutcome.Saved => Ok(result.Entry),
            SaveEntryOutcome.Rejected => BadRequest(Failure(result.Reason ?? "That change was rejected.")),
            _ => NotFound()
        };
    }

    /// <summary>
    /// Move a meal to another day or slot, swapping with whatever is already there.
    ///
    /// Separate from the edit above because the two want opposite things from an occupied
    /// destination: an edit replaces it, a drag must not throw away a meal the person was not
    /// even pointing at.
    /// </summary>
    [HttpPut("{planId:guid}/entries/{entryId:guid}/move")]
    [ProducesResponseType(typeof(MealPlanEntryMoveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveEntry(
        [FromRoute] Guid planId,
        [FromRoute] Guid entryId,
        [FromBody] MealPlanEntryMoveRequest request,
        CancellationToken ct)
    {
        var result = await _saveEntryUseCase.MoveAsync(planId, entryId, UserId, request, ct);
        return result.Outcome switch
        {
            SaveEntryOutcome.Saved => Ok(new MealPlanEntryMoveResponse
            {
                Entry = result.Entry!,
                Swapped = result.Swapped
            }),
            SaveEntryOutcome.Rejected => BadRequest(Failure(result.Reason ?? "That move was rejected.")),
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
        var result = await _saveEntryUseCase.DeleteAsync(planId, entryId, UserId, ct);
        return result.Outcome == SaveEntryOutcome.Saved ? NoContent() : NotFound();
    }

}
