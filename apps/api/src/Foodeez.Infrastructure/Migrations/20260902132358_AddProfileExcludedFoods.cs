using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileExcludedFoods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExcludedFoods",
                table: "user_profiles",
                type: "text",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            // MySQL fills a new NOT NULL text column with an empty string, which is not JSON.
            // Every existing profile would come back as a parse failure without this.
            migrationBuilder.Sql(
                "UPDATE user_profiles SET ExcludedFoods = '[]' WHERE ExcludedFoods = '' OR ExcludedFoods IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludedFoods",
                table: "user_profiles");
        }
    }
}
