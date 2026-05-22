using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "food_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Brand = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServingSize = table.Column<float>(type: "float", nullable: false),
                    ServingUnit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Barcode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCustom = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    nutrition_calories = table.Column<float>(type: "float", nullable: false),
                    nutrition_protein = table.Column<float>(type: "float", nullable: false),
                    nutrition_carbohydrates = table.Column<float>(type: "float", nullable: false),
                    nutrition_fat = table.Column<float>(type: "float", nullable: false),
                    nutrition_fiber = table.Column<float>(type: "float", nullable: false),
                    nutrition_sugar = table.Column<float>(type: "float", nullable: false),
                    nutrition_sodium = table.Column<float>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food_items", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instructions = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrepTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    CookTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    Servings = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAIGenerated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    nutrition_calories = table.Column<float>(type: "float", nullable: false),
                    nutrition_protein = table.Column<float>(type: "float", nullable: false),
                    nutrition_carbohydrates = table.Column<float>(type: "float", nullable: false),
                    nutrition_fat = table.Column<float>(type: "float", nullable: false),
                    nutrition_fiber = table.Column<float>(type: "float", nullable: false),
                    nutrition_sugar = table.Column<float>(type: "float", nullable: false),
                    nutrition_sodium = table.Column<float>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "recipe_ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RecipeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FoodItemId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Quantity = table.Column<float>(type: "float", nullable: false),
                    Unit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recipe_ingredients_food_items_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "food_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recipe_ingredients_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MealType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsAIGenerated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_plans_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    HeightCm = table.Column<float>(type: "float", nullable: false),
                    WeightKg = table.Column<float>(type: "float", nullable: false),
                    TargetWeightKg = table.Column<float>(type: "float", nullable: true),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    ActivityLevel = table.Column<int>(type: "int", nullable: false),
                    DietaryGoal = table.Column<int>(type: "int", nullable: false),
                    DailyCalorieTarget = table.Column<int>(type: "int", nullable: false),
                    DailyProteinTargetG = table.Column<float>(type: "float", nullable: false),
                    DailyCarbTargetG = table.Column<float>(type: "float", nullable: false),
                    DailyFatTargetG = table.Column<float>(type: "float", nullable: false),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProfileCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_log_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MealLogId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FoodItemId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Quantity = table.Column<float>(type: "float", nullable: false),
                    Unit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nutrition_calories = table.Column<float>(type: "float", nullable: false),
                    nutrition_protein = table.Column<float>(type: "float", nullable: false),
                    nutrition_carbohydrates = table.Column<float>(type: "float", nullable: false),
                    nutrition_fat = table.Column<float>(type: "float", nullable: false),
                    nutrition_fiber = table.Column<float>(type: "float", nullable: false),
                    nutrition_sugar = table.Column<float>(type: "float", nullable: false),
                    nutrition_sodium = table.Column<float>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_log_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_log_items_food_items_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "food_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meal_log_items_meal_logs_MealLogId",
                        column: x => x.MealLogId,
                        principalTable: "meal_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_plan_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MealPlanId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MealType = table.Column<int>(type: "int", nullable: false),
                    RecipeId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    FoodItemId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Servings = table.Column<float>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_plan_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_plan_entries_food_items_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "food_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_meal_plan_entries_meal_plans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalTable: "meal_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_plan_entries_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_food_items_Barcode",
                table: "food_items",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_meal_log_items_FoodItemId",
                table: "meal_log_items",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_log_items_MealLogId",
                table: "meal_log_items",
                column: "MealLogId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_logs_UserId_LogDate",
                table: "meal_logs",
                columns: new[] { "UserId", "LogDate" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_plan_entries_FoodItemId",
                table: "meal_plan_entries",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_plan_entries_MealPlanId",
                table: "meal_plan_entries",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_plan_entries_RecipeId",
                table: "meal_plan_entries",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_plans_UserId",
                table: "meal_plans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_recipe_ingredients_FoodItemId",
                table: "recipe_ingredients",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_recipe_ingredients_RecipeId",
                table: "recipe_ingredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_UserId",
                table: "user_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meal_log_items");

            migrationBuilder.DropTable(
                name: "meal_plan_entries");

            migrationBuilder.DropTable(
                name: "recipe_ingredients");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "meal_logs");

            migrationBuilder.DropTable(
                name: "meal_plans");

            migrationBuilder.DropTable(
                name: "food_items");

            migrationBuilder.DropTable(
                name: "recipes");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
