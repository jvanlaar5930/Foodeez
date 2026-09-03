using Foodeez.API.Streaming;
using Foodeez.Application.Common;
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
    private readonly ParseMealImageUseCase _parseMealImageUseCase;
    private readonly QuickAddMealUseCase _quickAddMealUseCase;
    private readonly GetDailyLogsUseCase _getDailyLogsUseCase;
    private readonly GetMealLogsRangeUseCase _getMealLogsRangeUseCase;
    private readonly GetNutritionSummaryUseCase _getNutritionSummaryUseCase;
    private readonly UpdateMealLogUseCase _updateMealLogUseCase;
    private readonly DeleteMealLogUseCase _deleteMealLogUseCase;
    private readonly AnalyzeMealLogUseCase _analyzeMealLogUseCase;
    private readonly AnalyzeDayUseCase _analyzeDayUseCase;

    public MealLogsController(
        LogMealUseCase logMealUseCase,
        ParseMealImageUseCase parseMealImageUseCase,
        QuickAddMealUseCase quickAddMealUseCase,
        GetDailyLogsUseCase getDailyLogsUseCase,
        GetMealLogsRangeUseCase getMealLogsRangeUseCase,
        GetNutritionSummaryUseCase getNutritionSummaryUseCase,
        UpdateMealLogUseCase updateMealLogUseCase,
        DeleteMealLogUseCase deleteMealLogUseCase,
        AnalyzeMealLogUseCase analyzeMealLogUseCase,
        AnalyzeDayUseCase analyzeDayUseCase)
    {
        _logMealUseCase = logMealUseCase;
        _parseMealImageUseCase = parseMealImageUseCase;
        _quickAddMealUseCase = quickAddMealUseCase;
        _getDailyLogsUseCase = getDailyLogsUseCase;
        _getMealLogsRangeUseCase = getMealLogsRangeUseCase;
        _getNutritionSummaryUseCase = getNutritionSummaryUseCase;
        _updateMealLogUseCase = updateMealLogUseCase;
        _deleteMealLogUseCase = deleteMealLogUseCase;
        _analyzeMealLogUseCase = analyzeMealLogUseCase;
        _analyzeDayUseCase = analyzeDayUseCase;
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

    /// <summary>Update an existing meal log and replace its items.</summary>
    [HttpPut("{mealLogId:guid}")]
    [ProducesResponseType(typeof(MealLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMealLog(Guid mealLogId, [FromBody] LogMealRequest request)
    {
        var result = await _updateMealLogUseCase.ExecuteAsync(mealLogId, request);
        return Ok(result);
    }

    /// <summary>
    /// Get the AI analysis of a logged meal. The analysis is generated and stored on first
    /// request and served from storage afterwards; pass refresh=true to force a new one, and
    /// note that editing the meal's items discards the old score by itself.
    /// </summary>
    [HttpPost("{mealLogId:guid}/analysis")]
    [ProducesResponseType(typeof(MealAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeMealLog(Guid mealLogId, [FromQuery] bool refresh, CancellationToken ct)
    {
        var analysis = await _analyzeMealLogUseCase.ExecuteAsync(mealLogId, refresh, ct);
        return Ok(analysis);
    }

    /// <summary>
    /// The same analysis, streamed as server-sent events: "delta" events as the model writes,
    /// then a "result" event with the stored analysis. An analysis already on file arrives as
    /// a single result with no deltas.
    /// </summary>
    [HttpPost("{mealLogId:guid}/analysis/stream")]
    [Produces("text/event-stream")]
    public async Task AnalyzeMealLogStream(Guid mealLogId, [FromQuery] bool refresh, CancellationToken ct)
    {
        await ServerSentEventStream.WriteAsync(
            Response, _analyzeMealLogUseCase.ExecuteStreamAsync(mealLogId, refresh, ct), ct);
    }

    /// <summary>Delete an existing meal log.</summary>
    [HttpDelete("{mealLogId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMealLog(Guid mealLogId)
    {
        await _deleteMealLogUseCase.ExecuteAsync(mealLogId);
        return NoContent();
    }

    /// <summary>
    /// Read a meal out of a description - "turkey sandwich on rye with mayo, and an apple" -
    /// as the separate foods it is made of, matched to the food database where possible.
    ///
    /// Nothing is logged: the items come back for the user to look over and save themselves.
    /// A description that exactly names one of their saved meals is answered from it, with no
    /// AI call at all.
    /// </summary>
    [HttpPost("quick-add")]
    [ProducesResponseType(typeof(QuickAddResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QuickAdd([FromBody] QuickAddRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "Describe the meal first." });

        var userId = CurrentUser.IdOf(User);
        if (userId is null)
            return Unauthorized();

        // The body carries a userId for symmetry with the rest of this controller, but the
        // foods created along the way are filed against the token's user, never the body's.
        request.UserId = userId.Value;

        return Ok(await _quickAddMealUseCase.ExecuteAsync(request, ct));
    }

    /// <summary>
    /// Read a meal out of a photograph, as the separate foods on the plate. Same result shape
    /// as quick add, and the same rule: nothing is logged until the user saves it.
    /// </summary>
    [HttpPost("parse-image")]
    [ProducesResponseType(typeof(QuickAddResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ParseFoodImage(IFormFile image, CancellationToken ct)
    {
        if (image == null || image.Length == 0)
            return BadRequest("No image file provided.");

        var userId = CurrentUser.IdOf(User);
        if (userId is null)
            return Unauthorized();

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var imageData = ms.ToArray();
        var mimeType = image.ContentType;

        var result = await _parseMealImageUseCase.ExecuteAsync(userId.Value, imageData, mimeType, ct);
        return Ok(result);
    }

    /// <summary>Get all meal logs for the signed-in user on a specific date.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MealLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyLogs([FromQuery] string date)
    {
        if (CurrentUser.IdOf(User) is not { } userId)
            return Unauthorized();

        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        var logs = await _getDailyLogsUseCase.ExecuteAsync(userId, parsedDate);
        return Ok(logs);
    }

    /// <summary>
    /// Get all meal logs for the signed-in user across a date range - used to show what was actually
    /// eaten alongside a calendar of what was planned.
    /// </summary>
    [HttpGet("range")]
    [ProducesResponseType(typeof(List<MealLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMealLogsRange(
        [FromQuery] string startDate, [FromQuery] string endDate)
    {
        if (CurrentUser.IdOf(User) is not { } userId)
            return Unauthorized();

        if (!DateOnly.TryParse(startDate, out var parsedStart) || !DateOnly.TryParse(endDate, out var parsedEnd))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        var logs = await _getMealLogsRangeUseCase.ExecuteAsync(userId, parsedStart, parsedEnd);
        return Ok(logs);
    }

    /// <summary>
    /// The stored AI analysis of a day, or 204 when there is none yet for the day as it now
    /// stands. Never calls the AI provider, so a page can ask for it on every load.
    /// </summary>
    [HttpGet("day-analysis")]
    [ProducesResponseType(typeof(DayAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetDayAnalysis([FromQuery] string date)
    {
        if (CurrentUser.IdOf(User) is not { } userId)
            return Unauthorized();

        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        var analysis = await _analyzeDayUseCase.PeekAsync(userId, parsedDate);
        return analysis == null ? NoContent() : Ok(analysis);
    }

    /// <summary>
    /// Analyze a whole day of meals: how it stands against the user's targets and what to eat
    /// to round it out. The result is stored and reused until the day's meals or targets
    /// change; pass refresh=true to spend another AI call on a fresh one.
    /// </summary>
    [HttpPost("day-analysis")]
    [ProducesResponseType(typeof(DayAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AnalyzeDay(
        [FromQuery] string date,
        [FromQuery] bool refresh,
        CancellationToken ct)
    {
        if (CurrentUser.IdOf(User) is not { } userId)
            return Unauthorized();

        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        // InvalidOperationException -> 400 and AIGenerationFailedException -> 503 are both
        // mapped by ExceptionHandlingMiddleware.
        return Ok(await _analyzeDayUseCase.ExecuteAsync(userId, parsedDate, refresh, ct));
    }

    /// <summary>
    /// The same day analysis, streamed as server-sent events: "delta" events as the model
    /// writes, then a "result" event with the stored analysis.
    /// </summary>
    [HttpPost("day-analysis/stream")]
    [Produces("text/event-stream")]
    public async Task AnalyzeDayStream(
        [FromQuery] string date,
        [FromQuery] bool refresh,
        CancellationToken ct)
    {
        if (CurrentUser.IdOf(User) is not { } userId)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await ServerSentEventStream.WriteAsync(
            Response, _analyzeDayUseCase.ExecuteStreamAsync(userId, parsedDate, refresh, ct), ct);
    }

    /// <summary>Get the signed-in user's nutritional summary for a date, including progress toward targets.</summary>
    [HttpGet("nutrition-summary")]
    [ProducesResponseType(typeof(NutritionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNutritionSummary([FromQuery] string date)
    {
        if (CurrentUser.IdOf(User) is not { } userId)
            return Unauthorized();

        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        var summary = await _getNutritionSummaryUseCase.ExecuteAsync(userId, parsedDate);
        return Ok(summary);
    }
}
