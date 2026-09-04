using Foodeez.API.Streaming;
using Foodeez.Application.DTOs.Grocery;
using Foodeez.Application.UseCases.Grocery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

/// <summary>
/// The grocery tab: what a stretch of the meal plan adds up to in the shop, and the edits
/// made to it while shopping.
/// </summary>
[Route("api/grocery-lists")]
[Authorize]
public class GroceryListsController : FoodeezController
{
    private readonly GenerateGroceryListUseCase _generate;
    private readonly EditGroceryListUseCase _edit;

    public GroceryListsController(GenerateGroceryListUseCase generate, EditGroceryListUseCase edit)
    {
        _generate = generate;
        _edit = edit;
    }

    /// <summary>
    /// The list stored for a range, and how many meals are planned in it. Costs no AI call,
    /// so the tab can ask on every load and whenever the range changes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GroceryListStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetList([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
    {
        var userId = UserId;

        if (endDate < startDate)
        {
            return BadRequest(Failure("Pick an end date on or after the start date."));
        }

        var list = await _generate.PeekAsync(userId, startDate, endDate);

        return Ok(new GroceryListStateDto
        {
            List = list,
            PlannedMealCount = list?.PlannedMealCount
                ?? await _generate.CountPlannedMealsAsync(userId, startDate, endDate)
        });
    }

    /// <summary>
    /// Compile the list, streamed as server-sent events. A list already compiled from
    /// exactly these meals arrives complete with no deltas; only "refresh" spends an AI call
    /// on a range that has not changed.
    /// </summary>
    [HttpPost("generate/stream")]
    [Produces("text/event-stream")]
    public async Task GenerateStream([FromBody] GenerateGroceryListRequest request, CancellationToken ct)
    {
        await ServerSentEventStream.WriteAsync(
            Response, _generate.ExecuteStreamAsync(UserId, request, ct), ct);
    }

    /// <summary>Add something the plan never knew about.</summary>
    [HttpPost("{listId:guid}/items")]
    [ProducesResponseType(typeof(GroceryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
        [FromRoute] Guid listId, [FromBody] GroceryItemRequest request, CancellationToken ct)
    {
        // 200 rather than 201: the item has no address of its own to point a Location header
        // at - it is only ever read as part of the list it belongs to.
        var item = await _edit.AddAsync(UserId, listId, request, ct);
        return item == null ? NotFound() : Ok(item);
    }

    /// <summary>Change one line - the swap, when the shop had something else.</summary>
    [HttpPut("{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(GroceryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(
        [FromRoute] Guid listId,
        [FromRoute] Guid itemId,
        [FromBody] GroceryItemRequest request,
        CancellationToken ct)
    {
        var item = await _edit.UpdateAsync(UserId, listId, itemId, request, ct);
        return item == null ? NotFound() : Ok(item);
    }

    /// <summary>Tick one line off, or back on.</summary>
    [HttpPatch("{listId:guid}/items/{itemId:guid}/checked")]
    [ProducesResponseType(typeof(GroceryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetChecked(
        [FromRoute] Guid listId,
        [FromRoute] Guid itemId,
        [FromBody] SetGroceryItemCheckedRequest request,
        CancellationToken ct)
    {
        var item = await _edit.SetCheckedAsync(UserId, listId, itemId, request.IsChecked, ct);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpDelete("{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(
        [FromRoute] Guid listId, [FromRoute] Guid itemId, CancellationToken ct)
    {
        return await _edit.DeleteAsync(UserId, listId, itemId, ct) ? NoContent() : NotFound();
    }
}
