using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "analysis_completeness",
                table: "meal_logs",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "analysis_fingerprint",
                table: "meal_logs",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "analysis_generated_at",
                table: "meal_logs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "analysis_missing",
                table: "meal_logs",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "analysis_score",
                table: "meal_logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "analysis_suggestions",
                table: "meal_logs",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "analysis_completeness",
                table: "meal_logs");

            migrationBuilder.DropColumn(
                name: "analysis_fingerprint",
                table: "meal_logs");

            migrationBuilder.DropColumn(
                name: "analysis_generated_at",
                table: "meal_logs");

            migrationBuilder.DropColumn(
                name: "analysis_missing",
                table: "meal_logs");

            migrationBuilder.DropColumn(
                name: "analysis_score",
                table: "meal_logs");

            migrationBuilder.DropColumn(
                name: "analysis_suggestions",
                table: "meal_logs");
        }
    }
}
