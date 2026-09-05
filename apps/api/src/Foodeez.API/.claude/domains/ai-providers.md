# Domain: ai-providers

## Purpose

Every AI capability in the app - meal photo parsing, free-text parsing, nutrition estimation,
meal and day analysis, dietary recommendations, meal-plan generation, grocery-list compilation
and chat - goes through one interface pair with a provider chosen at runtime.

## Key Files

- `apps/api/src/Foodeez.Application/Interfaces/Services/{IAIService,IStreamingAIService}.cs`
- `apps/api/src/Foodeez.Infrastructure/Services/DynamicAIService.cs` - the registered
  implementation of both interfaces; resolves the concrete provider by name.
- `apps/api/src/Foodeez.Infrastructure/Services/AIProviderBase.cs` - the shared envelope
  (prompt, parse, fallback, logging, cancellation).
- Providers: `ClaudeAIService`, `GeminiAIService`, `GroqAIService`, `OllamaAIService`,
  `LocalAIService` (any OpenAI-compatible server).
- `apps/api/src/Foodeez.Infrastructure/Services/{AIJson.cs,StreamingHttp.cs,Json/NullTolerantConverters.cs}`
- Prompts and stream types in `apps/api/src/Foodeez.Application/Common/`:
  `MealAnalysisPrompt`, `DayAnalysisPrompt`, `MealParsePrompt`, `MealPlanPrompt`,
  `GroceryListPrompt`, `NutritionChatPrompt`, `RecommendationsPrompt`, `NutritionEstimation`,
  `AIStreamEvent`, `AINarration`, `AIGenerationFailedException`.
- `apps/api/src/Foodeez.API/Streaming/ServerSentEventStream.cs`
- `apps/api/src/Foodeez.API/Controllers/AIController.cs` (`api/ai`).
- Clients: `apps/web/src/services/aiStream.ts`, `apps/mobile/src/services/aiStream.ts`.

## Main Flows

**Provider selection** - `DynamicAIService.ResolveAsync()` reads the `ai.provider` row from the
`AppSettings` table (default `claude`) and resolves the matching registered service. The result
is cached in the instance, and the instance is scoped, so one request resolves once. Accepted
names: `claude` (default/fallback), `gemini`, `groq`, `ollama`, and for `LocalAIService` the
aliases `local`, `lmstudio`, `lm-studio`, `llamacpp`, `llama.cpp`, `llama-cpp`, `localai`,
`vllm`, `jan`, `openai-compatible`.

**A blocking call** - `AIProviderBase.ExecuteAsync(prompt, parse, fallback, activity, ct)`
sends one prompt via the subclass's `SendAsync`, parses the answer, and on any failure logs
`"{Provider}: failed to {Activity}."` and returns the fallback. `OperationCanceledException` is
rethrown, never logged as a provider fault - a cancelled call must not be flattened into empty
data the caller would treat as real.

**A streamed call** - the use case exposes `ExecuteStreamAsync` returning
`IAsyncEnumerable<AIStreamEvent>`; the controller passes it to `ServerSentEventStream.WriteAsync`,
which sets `text/event-stream`, `no-cache`, `X-Accel-Buffering: no`, disables response body
buffering, and emits `delta` / `done` / `error` events. After the first byte the status is
settled, so a mid-stream failure arrives as an `error` event rather than a 500.

Streaming endpoints today: `POST api/ai/analyze-meal/stream`,
`POST api/meal-logs/{id}/analysis/stream`, `POST api/meal-logs/day-analysis/stream`,
`POST api/meal-plans/generate/stream`, `POST api/grocery-lists/generate/stream`,
`POST api/chat/messages/stream`.

## Data and State

No AI state of its own. The active provider and the `local.*` overrides live in the
`AppSettings` table (Admin > Settings). Generated artifacts are cached by the domain that owns
them - `MealLog.Analysis`, `DayAnalysis`, `GroceryList`, `ChatMessage.Suggestions`.

## External Systems

Anthropic Claude (`Claude:ApiKey`, `Claude:Model`, `Claude:MaxTokens` - defaults to 8192 output
tokens), Google Gemini, Groq, Ollama (`Ollama:BaseUrl`, `Ollama:Model`), and any
OpenAI-compatible server (`LocalAI:BaseUrl`, `Model`, `ApiKey`, `SupportsVision`,
`TimeoutSeconds`, optional `MaxTokens`).

## Tests and Validation

- `apps/api/tests/Foodeez.Infrastructure.Tests/Services/AIProviderBaseTests.cs` - the envelope,
  including the cancellation rule.
- `apps/api/tests/Foodeez.Integration.Tests/AIServiceRegistrationTests.cs` - that
  `IAIService` and `IStreamingAIService` resolve to the same shared instance.
- `apps/api/tests/Foodeez.Application.Tests/Common/{MealPlanPromptTests,RecommendationsPromptTests,JsonExtractionTests}.cs`

## Known Gotchas

- **Never add a resilience/retry handler to an AI `HttpClient`.** A model call is not
  idempotent, is billed per attempt, can run for minutes, and a streaming body cannot be
  replayed. `AIProviderBase` already turns a failure into a usable fallback.
- `LocalAIService`'s `HttpClient.Timeout` is deliberately infinite; its deadline is applied per
  request from `LocalAI:TimeoutSeconds`.
- `LocalAI:MaxTokens` is intentionally unset - nothing here can know what a locally loaded
  model supports, so the server's own default is left in place.
- Provider failures usually return a fallback rather than throwing. If a caller needs the
  failure to be visible, it must throw `AIGenerationFailedException` itself (mapped to 503).
- A stream that stops silently is nearly always transport, not the model: Kestrel's
  `MinResponseDataRate` (disabled in `Program.cs`) or nginx's `proxy_read_timeout` /
  `proxy_buffering` (600s / off in `apps/web/nginx.conf`).
- Adding a capability means touching four places: the interface, `AIProviderBase`,
  `DynamicAIService`'s delegating member, and the prompt builder.
- Model responses are parsed with null-tolerant JSON converters (`AIJson`,
  `NullTolerantConverters`) because models omit or null fields unpredictably.
