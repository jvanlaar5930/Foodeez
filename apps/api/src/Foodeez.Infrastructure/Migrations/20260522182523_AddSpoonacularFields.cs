using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpoonacularFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AppLogs table already exists from the raw-SQL AddAppLogs migration — skip CreateTable.

            migrationBuilder.Sql("""
                ALTER TABLE `recipes`
                    ADD COLUMN `SpoonacularId`       INT          NULL,
                    ADD COLUMN `SpoonacularSyncedAt` DATETIME(6)  NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `recipe_ingredients`
                    DROP FOREIGN KEY `FK_recipe_ingredients_food_items_FoodItemId`;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `recipe_ingredients`
                    MODIFY COLUMN `FoodItemId`    CHAR(36)     NULL COLLATE ascii_general_ci,
                    ADD    COLUMN `IngredientName` LONGTEXT     NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `recipe_ingredients`
                    ADD CONSTRAINT `FK_recipe_ingredients_food_items_FoodItemId`
                    FOREIGN KEY (`FoodItemId`) REFERENCES `food_items` (`Id`)
                    ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `recipes`
                    DROP COLUMN `SpoonacularSyncedAt`,
                    DROP COLUMN `SpoonacularId`;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `recipe_ingredients`
                    DROP FOREIGN KEY `FK_recipe_ingredients_food_items_FoodItemId`;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `recipe_ingredients`
                    DROP  COLUMN  `IngredientName`,
                    MODIFY COLUMN `FoodItemId` CHAR(36) NOT NULL COLLATE ascii_general_ci;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `recipe_ingredients`
                    ADD CONSTRAINT `FK_recipe_ingredients_food_items_FoodItemId`
                    FOREIGN KEY (`FoodItemId`) REFERENCES `food_items` (`Id`)
                    ON DELETE RESTRICT;
                """);
        }
    }
}
