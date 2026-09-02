using Foodeez.API.Streaming;
using Foodeez.Application.DTOs.Chat;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.UseCases.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

/// <summary>
/// The advice tab: a kept conversation with the nutrition assistant, and the way a plan that
/// comes out of one reaches the calendar.
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly GetConversationsUseCase _conversations;
    private readonly SendChatMessageUseCase _sendMessage;
    private readonly AcceptSuggestionsUseCase _acceptSuggestions;
    private readonly SaveChatRecipesUseCase _saveRecipes;

    public ChatController(
        GetConversationsUseCase conversations,
        SendChatMessageUseCase sendMessage,
        AcceptSuggestionsUseCase acceptSuggestions,
        SaveChatRecipesUseCase saveRecipes)
    {
        _conversations = conversations;
        _sendMessage = sendMessage;
        _acceptSuggestions = acceptSuggestions;
        _saveRecipes = saveRecipes;
    }

    /// <summary>Every thread this user has, newest activity first.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<ChatConversationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations()
    {
        if (CurrentUser.IdOf(User) is not { } userId) return Unauthorized();

        return Ok(await _conversations.ExecuteAsync(userId));
    }

    /// <summary>One thread, with everything said in it.</summary>
    [HttpGet("conversations/{conversationId:guid}")]
    [ProducesResponseType(typeof(ChatConversationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation([FromRoute] Guid conversationId)
    {
        if (CurrentUser.IdOf(User) is not { } userId) return Unauthorized();

        var conversation = await _conversations.GetAsync(userId, conversationId);
        return conversation == null ? NotFound() : Ok(conversation);
    }

    [HttpDelete("conversations/{conversationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation([FromRoute] Guid conversationId, CancellationToken ct)
    {
        if (CurrentUser.IdOf(User) is not { } userId) return Unauthorized();

        return await _conversations.DeleteAsync(userId, conversationId, ct) ? NoContent() : NotFound();
    }

    /// <summary>
    /// Ask a question. The answer is streamed as server-sent events - "delta" events carry
    /// the reply as it is written, and a final "result" event carries the saved message along
    /// with the thread it landed in, which is how a new conversation learns its own id.
    /// </summary>
    [HttpPost("messages/stream")]
    [Produces("text/event-stream")]
    public async Task SendMessageStream([FromBody] SendChatMessageRequest request, CancellationToken ct)
    {
        if (CurrentUser.IdOf(User) is not { } userId)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await ServerSentEventStream.WriteAsync(
            Response, _sendMessage.ExecuteStreamAsync(userId, request, ct), ct);
    }

    /// <summary>
    /// Put the meals a reply suggested onto the calendar. Returns the plans that ended up
    /// holding them, so the client can show the week without refetching everything.
    /// </summary>
    [HttpPost("messages/{messageId:guid}/plan")]
    [ProducesResponseType(typeof(List<MealPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptSuggestions(
        [FromRoute] Guid messageId,
        [FromBody] AcceptSuggestionsRequest? request,
        CancellationToken ct)
    {
        if (CurrentUser.IdOf(User) is not { } userId) return Unauthorized();

        var result = await _acceptSuggestions.ExecuteAsync(
            userId, messageId, request?.Indexes ?? new List<int>(), ct);

        return result.Outcome switch
        {
            AcceptOutcome.Added => Ok(result.Plans),
            AcceptOutcome.NothingToAdd => BadRequest(new { message = "That reply has no meals to add." }),
            _ => NotFound()
        };
    }

    /// <summary>
    /// Keep the recipes a reply wrote out. They join the shared recipe library and the
    /// caller's own saved collection, and come back as full recipes so the client can open
    /// one straight from the conversation.
    /// </summary>
    [HttpPost("messages/{messageId:guid}/recipes")]
    [ProducesResponseType(typeof(List<RecipeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveRecipes(
        [FromRoute] Guid messageId,
        [FromBody] SaveChatRecipesRequest? request,
        CancellationToken ct)
    {
        if (CurrentUser.IdOf(User) is not { } userId) return Unauthorized();

        var result = await _saveRecipes.ExecuteAsync(
            userId, messageId, request?.Indexes ?? new List<int>(), ct);

        return result.Outcome switch
        {
            SaveRecipesOutcome.Saved => Ok(result.Recipes),
            SaveRecipesOutcome.NothingToSave => BadRequest(new { message = "That reply has no recipes to save." }),
            _ => NotFound()
        };
    }
}
