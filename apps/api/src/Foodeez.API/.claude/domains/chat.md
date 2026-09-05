# Domain: chat

## Purpose

The AI nutrition assistant. Conversations are persisted, replies stream token by token, and an
assistant message can carry structured suggestions the user can accept - meals that become
meal-plan entries, or recipes that become saved recipes.

## Key Files

- `apps/api/src/Foodeez.API/Controllers/ChatController.cs` (route `api/chat`).
- `apps/api/src/Foodeez.Application/UseCases/Chat/` - `SendChatMessageUseCase`,
  `GetConversationsUseCase`, `AcceptSuggestionsUseCase`, `SaveChatRecipesUseCase`, `ChatMapper`.
- `apps/api/src/Foodeez.Application/Common/NutritionChatPrompt.cs`
- `apps/api/src/Foodeez.Domain/Entities/{ChatConversation,ChatMessage}.cs`,
  `apps/api/src/Foodeez.Domain/Enums/ChatRole.cs`,
  `apps/api/src/Foodeez.Domain/ValueObjects/{PlannedMeal,SuggestedRecipe}.cs`
- `apps/api/src/Foodeez.Infrastructure/Repositories/ChatRepository.cs`
- Clients: `apps/web/src/{components/chat,stores/chat.ts,services/chatService.ts}`,
  `apps/mobile/src/{components/chat,store/chatStore.ts,services/chatService.ts}`.

## Main Flows

**List and read** - `GET api/chat/conversations`,
`GET api/chat/conversations/{conversationId:guid}`,
`DELETE api/chat/conversations/{conversationId:guid}`.

**Send a message** - `POST api/chat/messages/stream` only; there is no blocking send. The user
message is stored, the prompt is built by `NutritionChatPrompt`, and the assistant reply is
streamed as server-sent events and stored when complete.
`ChatConversation.LastMessageAt` orders the list.

**Accept suggestions** - an assistant `ChatMessage` can carry `Suggestions`
(`List<PlannedMeal>`) and `Recipes` (`List<SuggestedRecipe>`), both stored as JSON columns.
`POST api/chat/messages/{messageId:guid}/plan` turns the suggestions into meal-plan entries
(`AcceptSuggestionsUseCase`) and stamps `SuggestionsAcceptedAt`;
`POST api/chat/messages/{messageId:guid}/recipes` saves the suggested recipes
(`SaveChatRecipesUseCase`) and stamps `RecipesSavedAt`. Those timestamps are how the UI knows to
show an already-accepted state instead of the button.

## Data and State

`ChatConversations` (user, title, `LastMessageAt`) and `ChatMessages` (`ChatRole`, content,
`Suggestions`, `SuggestionsAcceptedAt`, `Recipes`, `RecipesSavedAt`). Added by the
`AddChatAndGroceryLists` and `AddChatSuggestedRecipes` migrations. Configuration in
`Infrastructure/Data/Configurations/ChatConfiguration.cs`, with the JSON list converters.

## External Systems

The active AI provider through `IStreamingAIService` (see `domains/ai-providers.md`).

## Tests and Validation

`Needs verification` - no dedicated chat tests exist in `apps/api/tests`. The stream envelope
is covered indirectly by `AIProviderBaseTests`.

## Known Gotchas

- Conversations are private: the owner comes from the token (`UserId`), never from the request.
- The accept endpoints are idempotent by intent - check `SuggestionsAcceptedAt` /
  `RecipesSavedAt` rather than creating duplicates on a second call.
- A long assistant reply is exactly the case Kestrel's data-rate limit and nginx buffering would
  break; both are configured for it (see `domains/ai-providers.md`).
- Suggestions are JSON columns, not tables - querying inside them is not something the current
  model supports.
