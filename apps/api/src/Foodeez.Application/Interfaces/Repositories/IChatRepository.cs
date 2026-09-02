using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

/// <summary>
/// One thread as the list shows it. A projection rather than the entity because the list
/// needs a count and one line of the last reply, and loading every message of every thread
/// to work those out would grow with the length of the conversations, not the list.
/// </summary>
public sealed record ChatConversationSummary(
    Guid Id,
    string Title,
    DateTime LastMessageAt,
    DateTime CreatedAt,
    int MessageCount,
    string? LastMessageContent);

public interface IChatRepository : IBaseRepository<ChatConversation>
{
    /// <summary>A user's threads, newest activity first.</summary>
    Task<IReadOnlyList<ChatConversationSummary>> GetSummariesAsync(Guid userId);

    /// <summary>One thread with its messages in the order they were written.</summary>
    Task<ChatConversation?> GetWithMessagesAsync(Guid conversationId);

    Task AddMessageAsync(ChatMessage message);

    /// <summary>One message with the thread it belongs to, for checking who owns it.</summary>
    Task<ChatMessage?> GetMessageAsync(Guid messageId);
}
