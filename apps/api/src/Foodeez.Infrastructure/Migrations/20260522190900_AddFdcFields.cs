using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFdcFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FdcId",
                table: "food_items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FdcSyncedAt",
                table: "food_items",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FdcId",
                table: "food_items");

            migrationBuilder.DropColumn(
                name: "FdcSyncedAt",
                table: "food_items");
        }
    }
}
