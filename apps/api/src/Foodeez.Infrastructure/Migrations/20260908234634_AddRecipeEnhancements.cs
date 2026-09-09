using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EnhancedAt",
                table: "recipes",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EnhancedFromRecipeId",
                table: "recipes",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "EnhancementNotes",
                table: "recipes",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_enhanced_from_user",
                table: "recipes",
                columns: new[] { "EnhancedFromRecipeId", "CreatedByUserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_recipes_recipes_EnhancedFromRecipeId",
                table: "recipes",
                column: "EnhancedFromRecipeId",
                principalTable: "recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recipes_recipes_EnhancedFromRecipeId",
                table: "recipes");

            migrationBuilder.DropIndex(
                name: "ix_recipes_enhanced_from_user",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "EnhancedAt",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "EnhancedFromRecipeId",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "EnhancementNotes",
                table: "recipes");
        }
    }
}
