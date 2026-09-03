using Foodeez.Domain.Common;

namespace Foodeez.Domain.Entities;

/// <summary>
/// One thread of nutrition advice. Conversations are kept rather than held in the page,
/// because the assistant's usefulness comes from what it has already been told - and because
/// the plans and shopping lists that come out of a thread are worth being able to go back to.
/// </summary>
public class ChatConversation : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>Taken from the opening question, so a list of threads reads as a list of topics.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Sorts the list. CreatedAt would bury a thread that is still being used.</summary>
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
