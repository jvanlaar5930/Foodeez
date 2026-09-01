using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeSourceAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DetailFetchedAt",
                table: "recipes",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "recipes",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "recipes",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetailFetchedAt",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "recipes");
        }
    }
}
