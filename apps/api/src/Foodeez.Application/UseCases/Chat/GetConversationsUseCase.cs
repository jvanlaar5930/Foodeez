using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Chat;

namespace Foodeez.Application.UseCases.Chat;

/// <summary>Reading advice threads: the list, and one thread in full.</summary>
public class GetConversationsUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationsUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ChatConversationDto>> ExecuteAsync(Guid userId)
    {
        var conversations = await _unitOfWork.Chat.GetSummariesAsync(userId);
        return conversations.Select(ChatMapper.ToSummary).ToList();
    }

    /// <summary>
    /// One thread with its messages, or null when it is missing or belongs to someone else.
    /// Those two are deliberately the same answer: confirming a thread exists would tell an
    /// unauthorised caller something they should not learn.
    /// </summary>
    public async Task<ChatConversationDetailDto?> GetAsync(Guid userId, Guid conversationId)
    {
        var conversation = await _unitOfWork.Chat.GetWithMessagesAsync(conversationId);
        return conversation == null || conversation.UserId != userId
            ? null
            : ChatMapper.ToDetail(conversation);
    }

    /// <summary>True when the thread existed and was deleted.</summary>
    public async Task<bool> DeleteAsync(Guid userId, Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await _unitOfWork.Chat.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != userId)
        {
            return false;
        }

        // Messages go with it: the configuration cascades the delete.
        _unitOfWork.Chat.Delete(conversation);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
