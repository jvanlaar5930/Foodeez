using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meal_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MealType = table.Column<int>(type: "int", nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_templates_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "meal_template_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MealTemplateId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_meal_template_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_template_items_food_items_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "food_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meal_template_items_meal_templates_MealTemplateId",
                        column: x => x.MealTemplateId,
                        principalTable: "meal_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_meal_template_items_FoodItemId",
                table: "meal_template_items",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_template_items_MealTemplateId",
                table: "meal_template_items",
                column: "MealTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_templates_UserId_Name",
                table: "meal_templates",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meal_template_items");

            migrationBuilder.DropTable(
                name: "meal_templates");
        }
    }
}
