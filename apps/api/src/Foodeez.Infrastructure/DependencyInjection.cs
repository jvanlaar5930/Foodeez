using Foodeez.Application.Common;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Infrastructure.Data;
using Foodeez.Infrastructure.Repositories;
using Foodeez.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Foodeez.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        // ── Repositories ──────────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMealLogRepository, MealLogRepository>();
        services.AddScoped<IDayAnalysisRepository, DayAnalysisRepository>();
        services.AddScoped<IFoodItemRepository, FoodItemRepository>();
        services.AddScoped<IMealTemplateRepository, MealTemplateRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<ISavedRecipeRepository, SavedRecipeRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IGroceryListRepository, GroceryListRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IAppLogRepository, AppLogRepository>();

        // ── Unit of Work ──────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── AI Services ──────────────────────────────────────────────────
        // Concrete implementations registered directly (DynamicAIService resolves by name)
        // Deliberately not resilience-wrapped. A model call is not idempotent, is billed per
        // attempt and can run for minutes, and the streaming ones hand back a response body
        // that cannot be replayed. AIProviderBase already turns a failure into a fallback the
        // caller can show, which is the right answer here - retrying silently would double the
        // cost of an outage and stall the request while doing it.
        services.AddHttpClient<ClaudeAIService>();
        services.AddHttpClient<GeminiAIService>();
        services.AddHttpClient<GroqAIService>();
        services.AddHttpClient<OllamaAIService>();
        // Self-hosted OpenAI-compatible servers (LM Studio, llama.cpp, vLLM, LocalAI, …). They can
        // spend minutes on one prompt, so the deadline is set per request instead of by HttpClient.
        services.AddHttpClient<LocalAIService>(client => client.Timeout = Timeout.InfiniteTimeSpan);
        // DynamicAIService is the active IAIService — reads provider from AppSettings at runtime
        // Registered once and shared by both interfaces, so streaming and non-streaming calls
        // in one request go to the same instance and reuse its resolved provider. The previous
        // form cast IAIService to IStreamingAIService, which would have thrown at runtime for
        // any provider that did not implement both.
        services.AddScoped<DynamicAIService>();
        services.AddScoped<IAIService>(sp => sp.GetRequiredService<DynamicAIService>());
        services.AddScoped<IStreamingAIService>(sp => sp.GetRequiredService<DynamicAIService>());

        // ── Spoonacular Service (typed HttpClient) ────────────────────────
        // Retried, unlike the AI clients above: these are idempotent GETs against a public
        // API, so a 503 or a dropped connection is worth trying again rather than surfacing
        // as an empty recipe search. The standard handler brings a retry with backoff and
        // jitter, a circuit breaker and a per-attempt timeout.
        services.AddHttpClient<ISpoonacularService, SpoonacularService>()
            .AddStandardResilienceHandler();

        // ── USDA FoodData Service (typed HttpClient) ──────────────────────
        services.AddHttpClient<IFoodDataService, UsdaFoodDataService>()
            .AddStandardResilienceHandler();

        // ── JWT Service ───────────────────────────────────────────────────
        services.AddSingleton<IJwtService, JwtService>();

        // ── Notification Service ──────────────────────────────────────────
        services.AddScoped<INotificationService, NotificationService>();

        // ── Database Logging ──────────────────────────────────────────────
        services.AddSingleton<LogQueue>();
        services.AddSingleton<IAppLogger, DbAppLogger>();
        services.AddHostedService<LogWriterService>();

        return services;
    }
}
