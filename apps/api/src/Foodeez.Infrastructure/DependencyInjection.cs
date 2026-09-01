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
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<ISavedRecipeRepository, SavedRecipeRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IAppLogRepository, AppLogRepository>();

        // ── Unit of Work ──────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── AI Services ──────────────────────────────────────────────────
        // Concrete implementations registered directly (DynamicAIService resolves by name)
        services.AddHttpClient<ClaudeAIService>();
        services.AddHttpClient<GeminiAIService>();
        services.AddHttpClient<GroqAIService>();
        services.AddHttpClient<OllamaAIService>();
        // Self-hosted OpenAI-compatible servers (LM Studio, llama.cpp, vLLM, LocalAI, …). They can
        // spend minutes on one prompt, so the deadline is set per request instead of by HttpClient.
        services.AddHttpClient<LocalAIService>(client => client.Timeout = Timeout.InfiniteTimeSpan);
        // DynamicAIService is the active IAIService — reads provider from AppSettings at runtime
        services.AddScoped<IAIService, DynamicAIService>();

        // ── Spoonacular Service (typed HttpClient) ────────────────────────
        services.AddHttpClient<ISpoonacularService, SpoonacularService>();

        // ── USDA FoodData Service (typed HttpClient) ──────────────────────
        services.AddHttpClient<IFoodDataService, UsdaFoodDataService>();

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
