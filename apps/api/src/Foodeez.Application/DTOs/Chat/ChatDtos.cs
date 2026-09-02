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
    public List<SuggestedRecipeDto> Recipes { get; set; } = new();
    public DateTime? RecipesSavedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>A recipe the assistant wrote out and offered to keep.</summary>
public class SuggestedRecipeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; } = 1;
    public string? Tags { get; set; }
    public List<SuggestedRecipeIngredientDto> Ingredients { get; set; } = new();

    /// <summary>Per serving, and the model's estimate rather than a measurement.</summary>
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbohydrates { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
    public float Sugar { get; set; }
    public float Sodium { get; set; }
}

public class SuggestedRecipeIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
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

/// <summary>Which of a reply's recipes to keep in the library.</summary>
public class SaveChatRecipesRequest
{
    /// <summary>Empty means all of them, matching the single "Save recipe" button.</summary>
    public List<int> Indexes { get; set; } = new();
}
