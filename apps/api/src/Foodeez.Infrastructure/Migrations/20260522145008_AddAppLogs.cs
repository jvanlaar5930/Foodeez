using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodeez.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE `AppLogs` (
                    `Id`               BIGINT        NOT NULL AUTO_INCREMENT,
                    `Timestamp`        DATETIME(6)   NOT NULL,
                    `Level`            VARCHAR(16)   NOT NULL,
                    `Message`          LONGTEXT      NOT NULL,
                    `Source`           LONGTEXT      NULL,
                    `ExceptionType`    LONGTEXT      NULL,
                    `ExceptionMessage` LONGTEXT      NULL,
                    `StackTrace`       LONGTEXT      NULL,
                    `RequestMethod`    VARCHAR(10)   NULL,
                    `RequestPath`      VARCHAR(512)  NULL,
                    `StatusCode`       INT           NULL,
                    `UserId`           VARCHAR(36)   NULL,
                    `AdditionalData`   LONGTEXT      NULL,
                    PRIMARY KEY (`Id`),
                    INDEX `IX_AppLogs_Level`     (`Level`),
                    INDEX `IX_AppLogs_Timestamp` (`Timestamp`)
                ) CHARACTER SET=utf8mb4;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `AppLogs`;");
        }
    }
}
