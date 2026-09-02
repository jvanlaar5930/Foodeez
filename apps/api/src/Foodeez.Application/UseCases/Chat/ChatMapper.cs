using Foodeez.Application.DTOs.Chat;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.Chat;

/// <summary>Conversations and their messages, in the shape the clients read.</summary>
internal static class ChatMapper
{
    /// <summary>How much of the last reply the thread list shows.</summary>
    private const int PreviewLength = 140;

    public static ChatConversationDto ToSummary(ChatConversationSummary conversation) => new()
    {
        Id = conversation.Id,
        Title = conversation.Title,
        LastMessageAt = conversation.LastMessageAt,
        CreatedAt = conversation.CreatedAt,
        MessageCount = conversation.MessageCount,
        Preview = conversation.LastMessageContent == null
            ? null
            : Shorten(conversation.LastMessageContent)
    };

    public static ChatConversationDetailDto ToDetail(ChatConversation conversation) => new()
    {
        Id = conversation.Id,
        Title = conversation.Title,
        LastMessageAt = conversation.LastMessageAt,
        CreatedAt = conversation.CreatedAt,
        Messages = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(ToDto)
            .ToList()
    };

    public static ChatMessageDto ToDto(ChatMessage message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        Role = message.Role,
        Content = message.Content,
        Suggestions = message.Suggestions.Select(ToDto).ToList(),
        SuggestionsAcceptedAt = message.SuggestionsAcceptedAt,
        Recipes = message.Recipes.Select(ToDto).ToList(),
        RecipesSavedAt = message.RecipesSavedAt,
        CreatedAt = message.CreatedAt
    };

    public static PlannedMealDto ToDto(PlannedMeal meal) => new()
    {
        Date = meal.Date,
        MealType = meal.MealType,
        Name = meal.Name,
        Description = meal.Description,
        Servings = meal.Servings
    };

    public static PlannedMeal ToEntity(PlannedMealDto meal) => new()
    {
        Date = meal.Date,
        MealType = meal.MealType,
        Name = meal.Name,
        Description = meal.Description,
        Servings = meal.Servings
    };

    public static SuggestedRecipeDto ToDto(SuggestedRecipe recipe) => new()
    {
        Name = recipe.Name,
        Description = recipe.Description,
        Instructions = recipe.Instructions,
        PrepTimeMinutes = recipe.PrepTimeMinutes,
        CookTimeMinutes = recipe.CookTimeMinutes,
        Servings = recipe.Servings,
        Tags = recipe.Tags,
        Ingredients = recipe.Ingredients
            .Select(i => new SuggestedRecipeIngredientDto
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Notes = i.Notes
            })
            .ToList(),
        Calories = recipe.Calories,
        Protein = recipe.Protein,
        Carbohydrates = recipe.Carbohydrates,
        Fat = recipe.Fat,
        Fiber = recipe.Fiber,
        Sugar = recipe.Sugar,
        Sodium = recipe.Sodium
    };

    public static SuggestedRecipe ToEntity(SuggestedRecipeDto recipe) => new()
    {
        Name = recipe.Name,
        Description = recipe.Description,
        Instructions = recipe.Instructions,
        PrepTimeMinutes = recipe.PrepTimeMinutes,
        CookTimeMinutes = recipe.CookTimeMinutes,
        Servings = recipe.Servings,
        Tags = recipe.Tags,
        Ingredients = recipe.Ingredients
            .Select(i => new SuggestedRecipeIngredient
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Notes = i.Notes
            })
            .ToList(),
        Calories = recipe.Calories,
        Protein = recipe.Protein,
        Carbohydrates = recipe.Carbohydrates,
        Fat = recipe.Fat,
        Fiber = recipe.Fiber,
        Sugar = recipe.Sugar,
        Sodium = recipe.Sodium
    };

    private static string Shorten(string content)
    {
        var single = content.ReplaceLineEndings(" ").Trim();
        return single.Length <= PreviewLength ? single : single[..(PreviewLength - 3)].TrimEnd() + "...";
    }
}
