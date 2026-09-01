using System.Text;
using Foodeez.API.Middleware;
using Foodeez.Application.UseCases.AI;
using Foodeez.Application.UseCases.FoodItems;
using Foodeez.Application.UseCases.Recipes;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Foodeez.Application.UseCases.Auth;
using Foodeez.Application.UseCases.MealLogs;
using Foodeez.Application.UseCases.MealPlans;
using Foodeez.Application.UseCases.Users;
using Foodeez.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (DbContext, Repositories, Services) ────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Use Cases ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<RegisterUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<GetUserProfileUseCase>();
builder.Services.AddScoped<UpdateUserProfileUseCase>();
builder.Services.AddScoped<LogMealUseCase>();
builder.Services.AddScoped<UpdateMealLogUseCase>();
builder.Services.AddScoped<DeleteMealLogUseCase>();
builder.Services.AddScoped<AnalyzeMealLogUseCase>();
builder.Services.AddScoped<AnalyzeDayUseCase>();
builder.Services.AddScoped<ParseFoodImageUseCase>();
builder.Services.AddScoped<GetDailyLogsUseCase>();
builder.Services.AddScoped<GetNutritionSummaryUseCase>();
builder.Services.AddScoped<GetMealPlanUseCase>();
builder.Services.AddScoped<CreateMealPlanUseCase>();
builder.Services.AddScoped<GenerateAIMealPlanUseCase>();
builder.Services.AddScoped<GetDietaryRecommendationsUseCase>();
builder.Services.AddScoped<AnalyzeMealUseCase>();
builder.Services.AddScoped<EstimateNutritionUseCase>();
builder.Services.AddScoped<SearchRecipesUseCase>();
builder.Services.AddScoped<AutocompleteRecipesUseCase>();
builder.Services.AddScoped<GetRecipeDetailUseCase>();
builder.Services.AddScoped<SavedRecipesUseCase>();
builder.Services.AddScoped<SearchFoodItemsUseCase>();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key configuration is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Foodeez API",
        Version = "v1",
        Description = "Food tracking and meal prepping API with AI-powered nutritional analysis."
    });

    // Bearer token auth scheme for Swagger UI
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ═════════════════════════════════════════════════════════════════════════════

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Foodeez API v1"));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
