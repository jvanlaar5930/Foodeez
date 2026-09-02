using System.ComponentModel.DataAnnotations;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.Chat;

/// <summary>One thread in the list, without its messages.</summary>
public class ChatConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MessageCount { get; set; }

    /// <summary>The opening of the last reply, so the list reads like an inbox.</summary>
    public string? Preview { get; set; }
}

/// <summary>A thread with everything said in it.</summary>
public class ChatConversationDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<PlannedMealDto> Suggestions { get; set; } = new();
    public DateTime? SuggestionsAcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>A meal the assistant offered to put on the calendar.</summary>
public class PlannedMealDto
{
    public DateOnly Date { get; set; }
    public MealType MealType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public float Servings { get; set; } = 1f;
}

public class SendChatMessageRequest
{
    /// <summary>Omitted to start a new thread; the reply carries the id it was given.</summary>
    public Guid? ConversationId { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// What the client gets when a reply finishes: the reply itself, plus the thread id, which
/// is the only way a brand-new conversation learns what it is called.
/// </summary>
public class ChatReplyDto
{
    public Guid ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ChatMessageDto Message { get; set; } = new();
}

/// <summary>Which of a reply's suggestions to put on the calendar.</summary>
public class AcceptSuggestionsRequest
{
    /// <summary>Empty means all of them - the usual case, from a single "Add to plan" button.</summary>
    public List<int> Indexes { get; set; } = new();
}
