using Foodeez.Domain.Common;
using Foodeez.Domain.Enums;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Meals this reply proposed, empty for most turns. They stay on the message so the
    /// offer is still there when the thread is reopened days later.
    /// </summary>
    public List<PlannedMeal> Suggestions { get; set; } = new();

    /// <summary>
    /// Set once the reader has put the suggestions on their calendar, so the thread shows
    /// what was accepted instead of offering it again.
    /// </summary>
    public DateTime? SuggestionsAcceptedAt { get; set; }

    public ChatConversation Conversation { get; set; } = null!;
}
