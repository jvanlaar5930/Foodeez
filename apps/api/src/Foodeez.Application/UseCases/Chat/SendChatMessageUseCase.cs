using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Chat;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.UseCases.Chat;

/// <summary>
/// One turn of an advice conversation, streamed as the model writes it.
///
/// The question is saved before the model is called, not after: a reply that fails or is
/// abandoned half way leaves the thread showing what was asked, which is both what the reader
/// expects to see and what makes retrying possible.
/// </summary>
public class SendChatMessageUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStreamingAIService _streaming;

    public SendChatMessageUseCase(IUnitOfWork unitOfWork, IStreamingAIService streaming)
    {
        _unitOfWork = unitOfWork;
        _streaming = streaming;
    }

    public async IAsyncEnumerable<AIStreamEvent> ExecuteStreamAsync(
        Guid userId,
        SendChatMessageRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var question = request.Message.Trim();
        if (question.Length == 0)
        {
            yield return AIStreamEvent.Error("Ask a question to get advice.");
            yield break;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            yield return AIStreamEvent.Error("Your account could not be loaded. Please sign in again.");
            yield break;
        }

        var conversation = await ResolveConversationAsync(userId, request.ConversationId, question);
        if (conversation == null)
        {
            yield return AIStreamEvent.Error("That conversation could not be found.");
            yield break;
        }

        // The history the prompt sees is the thread as it stood before this question, which
        // is passed separately - so take the window first, then record the question.
        var history = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(NutritionChatPrompt.HistoryTurns)
            .ToList();

        await RecordAsync(
            conversation, ChatRole.User, question,
            new List<PlannedMealDto>(), new List<SuggestedRecipeDto>(), ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prompt = NutritionChatPrompt.Build(ProfileOf(user), history, question, today);

        // Two builders: the transcript holds everything, including the JSON the suggestions
        // are read from, while the prose is what the reader saw and so what gets stored as
        // the reply. Storing the transcript instead would put raw JSON in the thread.
        var transcript = new StringBuilder();
        var prose = new StringBuilder();

        await foreach (var delta in AINarration.NarrateAsync(_streaming, prompt, transcript, ct))
        {
            prose.Append(delta);
            yield return AIStreamEvent.Delta(delta);
        }

        var reply = prose.ToString().Trim();
        if (reply.Length == 0)
        {
            yield return AIStreamEvent.Error("The AI service could not answer right now. Please try again in a moment.");
            yield break;
        }

        var answer = transcript.ToString();
        var suggestions = NutritionChatPrompt.ParseSuggestions(answer, today);
        var recipes = NutritionChatPrompt.ParseRecipes(answer);
        var message = await RecordAsync(conversation, ChatRole.Assistant, reply, suggestions, recipes, ct);

        yield return AIStreamEvent.Result(new ChatReplyDto
        {
            ConversationId = conversation.Id,
            Title = conversation.Title,
            Message = ChatMapper.ToDto(message)
        });
    }

    /// <summary>
    /// The thread this turn belongs to: an existing one the user owns, or a new one named
    /// after the question. Returns null for a thread that is missing or someone else's.
    /// </summary>
    private async Task<ChatConversation?> ResolveConversationAsync(
        Guid userId, Guid? conversationId, string question)
    {
        if (conversationId is not { } id)
        {
            var created = new ChatConversation
            {
                UserId = userId,
                Title = NutritionChatPrompt.TitleFrom(question),
                LastMessageAt = DateTime.UtcNow
            };

            await _unitOfWork.Chat.AddAsync(created);
            return created;
        }

        var existing = await _unitOfWork.Chat.GetWithMessagesAsync(id);
        return existing == null || existing.UserId != userId ? null : existing;
    }

    private async Task<ChatMessage> RecordAsync(
        ChatConversation conversation,
        ChatRole role,
        string content,
        IReadOnlyList<PlannedMealDto> suggestions,
        IReadOnlyList<SuggestedRecipeDto> recipes,
        CancellationToken ct)
    {
        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = role,
            Content = content,
            Suggestions = suggestions.Select(ChatMapper.ToEntity).ToList(),
            Recipes = recipes.Select(ChatMapper.ToEntity).ToList()
        };

        conversation.Messages.Add(message);
        conversation.LastMessageAt = DateTime.UtcNow;

        await _unitOfWork.Chat.AddMessageAsync(message);
        await _unitOfWork.SaveChangesAsync(ct);

        return message;
    }

    /// <summary>
    /// The profile the advice is built on, or null when it has not been filled in - which is
    /// worth telling the model about rather than passing off as a profile full of zeroes.
    /// </summary>
    private static UserProfileDto? ProfileOf(User user)
    {
        if (user.Profile is not { } profile)
        {
            return null;
        }

        return new UserProfileDto
        {
            UserId = profile.UserId,
            HeightCm = profile.HeightCm,
            WeightKg = profile.WeightKg,
            TargetWeightKg = profile.TargetWeightKg,
            Age = profile.Age,
            Gender = profile.Gender,
            ActivityLevel = profile.ActivityLevel,
            DietaryGoal = profile.DietaryGoal,
            DailyCalorieTarget = profile.DailyCalorieTarget,
            DailyProteinTargetG = profile.DailyProteinTargetG,
            DailyCarbTargetG = profile.DailyCarbTargetG,
            DailyFatTargetG = profile.DailyFatTargetG,
            Notes = profile.Notes,
            ExcludedFoods = profile.ExcludedFoods.ToList(),
            ProfileCompleted = profile.ProfileCompleted
        };
    }
}
