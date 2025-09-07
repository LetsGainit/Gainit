using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNewAchievementTemplates_22Items : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AchievementIconUrl",
                table: "UserAchievements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IconUrl",
                table: "AchievementTemplates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AchievementIconUrl",
                table: "UserAchievements");

            migrationBuilder.AlterColumn<string>(
                name: "IconUrl",
                table: "AchievementTemplates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
        }
    }
}
