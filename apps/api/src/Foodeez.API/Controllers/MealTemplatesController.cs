using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.UseCases.MealLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

/// <summary>
/// Saved meals - the ones somebody eats over and over - and the recent meals they could be
/// made from. Every route reads the owner from the token: these are personal lists, and a
/// userId in the query string is only the caller's word for who they are.
/// </summary>
[ApiController]
[Route("api/meal-templates")]
[Authorize]
public class MealTemplatesController : ControllerBase
{
    private readonly MealTemplatesUseCase _useCase;

    public MealTemplatesController(MealTemplatesUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>The user's saved meals, most recently used first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MealTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var userId = CurrentUser.IdOf(User);
        if (userId is null) return Unauthorized();

        return Ok(await _useCase.ListAsync(userId.Value));
    }

    /// <summary>The user's last few distinct meals, for logging one of them again as it stands.</summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<RecentMealDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Recent()
    {
        var userId = CurrentUser.IdOf(User);
        if (userId is null) return Unauthorized();

        return Ok(await _useCase.RecentAsync(userId.Value));
    }

    /// <summary>Save the meal on screen under a name, replacing anything already saved under it.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MealTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Save([FromBody] SaveMealTemplateRequest request)
    {
        var userId = CurrentUser.IdOf(User);
        if (userId is null) return Unauthorized();

        request.UserId = userId.Value;

        var saved = await _useCase.SaveAsync(request);
        if (saved is null)
            return BadRequest(new { message = "Give the meal a name and at least one item." });

        return CreatedAtAction(nameof(List), new { }, saved);
    }

    /// <summary>The items of a saved meal, ready to drop into the meal dialog.</summary>
    [HttpPost("{templateId:guid}/apply")]
    [ProducesResponseType(typeof(QuickAddResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Apply(Guid templateId)
    {
        var userId = CurrentUser.IdOf(User);
        if (userId is null) return Unauthorized();

        var result = await _useCase.ApplyAsync(templateId, userId.Value);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{templateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid templateId)
    {
        var userId = CurrentUser.IdOf(User);
        if (userId is null) return Unauthorized();

        return await _useCase.DeleteAsync(templateId, userId.Value) ? NoContent() : NotFound();
    }
}
