using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class ChangedProjectAndUserFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TemplateProjectId",
                table: "TemplateProjects",
                newName: "ProjectId");

            migrationBuilder.AddColumn<string>(
                name: "Biography",
                table: "Users",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacebookPageURL",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubURL",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInURL",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureURL",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "TemplateProjects",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<List<string>>(
                name: "Goals",
                table: "TemplateProjects",
                type: "text[]",
                maxLength: 2000,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ProjectPictureUrl",
                table: "TemplateProjects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<List<string>>(
                name: "RequiredRoles",
                table: "TemplateProjects",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "Technologies",
                table: "TemplateProjects",
                type: "text[]",
                nullable: false);

            migrationBuilder.AlterColumn<int>(
                name: "DifficultyLevel",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "Projects",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<List<string>>(
                name: "Goals",
                table: "Projects",
                type: "text[]",
                maxLength: 2000,
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "ProgrammingLanguages",
                table: "Projects",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ProjectPictureUrl",
                table: "Projects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<List<string>>(
                name: "RequiredRoles",
                table: "Projects",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "Technologies",
                table: "Projects",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "NonprofitExpertiseExpertiseId",
                table: "Nonprofits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TechExpertiseExpertiseId",
                table: "Mentors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TechExpertiseExpertiseId",
                table: "Gainers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AchievementTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnlockCriteria = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserExpertises",
                columns: table => new
                {
                    ExpertiseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExpertises", x => x.ExpertiseId);
                    table.ForeignKey(
                        name: "FK_UserExpertises_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EarnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EarnedDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_AchievementTemplates_AchievementTemplateId",
                        column: x => x.AchievementTemplateId,
                        principalTable: "AchievementTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NonprofitExpertises",
                columns: table => new
                {
                    ExpertiseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldOfWork = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MissionStatement = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonprofitExpertises", x => x.ExpertiseId);
                    table.ForeignKey(
                        name: "FK_NonprofitExpertises_UserExpertises_ExpertiseId",
                        column: x => x.ExpertiseId,
                        principalTable: "UserExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechExpertises",
                columns: table => new
                {
                    ExpertiseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammingLanguages = table.Column<List<string>>(type: "text[]", nullable: false),
                    Technologies = table.Column<List<string>>(type: "text[]", nullable: false),
                    Tools = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechExpertises", x => x.ExpertiseId);
                    table.ForeignKey(
                        name: "FK_TechExpertises_UserExpertises_ExpertiseId",
                        column: x => x.ExpertiseId,
                        principalTable: "UserExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nonprofits_NonprofitExpertiseExpertiseId",
                table: "Nonprofits",
                column: "NonprofitExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_Mentors_TechExpertiseExpertiseId",
                table: "Mentors",
                column: "TechExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_Gainers_TechExpertiseExpertiseId",
                table: "Gainers",
                column: "TechExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementTemplateId",
                table: "UserAchievements",
                column: "AchievementTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementTemplateId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExpertises_UserId",
                table: "UserExpertises",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Gainers_TechExpertises_TechExpertiseExpertiseId",
                table: "Gainers",
                column: "TechExpertiseExpertiseId",
                principalTable: "TechExpertises",
                principalColumn: "ExpertiseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mentors_TechExpertises_TechExpertiseExpertiseId",
                table: "Mentors",
                column: "TechExpertiseExpertiseId",
                principalTable: "TechExpertises",
                principalColumn: "ExpertiseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nonprofits_NonprofitExpertises_NonprofitExpertiseExpertiseId",
                table: "Nonprofits",
                column: "NonprofitExpertiseExpertiseId",
                principalTable: "NonprofitExpertises",
                principalColumn: "ExpertiseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gainers_TechExpertises_TechExpertiseExpertiseId",
                table: "Gainers");

            migrationBuilder.DropForeignKey(
                name: "FK_Mentors_TechExpertises_TechExpertiseExpertiseId",
                table: "Mentors");

            migrationBuilder.DropForeignKey(
                name: "FK_Nonprofits_NonprofitExpertises_NonprofitExpertiseExpertiseId",
                table: "Nonprofits");

            migrationBuilder.DropTable(
                name: "NonprofitExpertises");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "TechExpertises");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "UserExpertises");

            migrationBuilder.DropTable(
                name: "AchievementTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Nonprofits_NonprofitExpertiseExpertiseId",
                table: "Nonprofits");

            migrationBuilder.DropIndex(
                name: "IX_Mentors_TechExpertiseExpertiseId",
                table: "Mentors");

            migrationBuilder.DropIndex(
                name: "IX_Gainers_TechExpertiseExpertiseId",
                table: "Gainers");

            migrationBuilder.DropColumn(
                name: "Biography",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FacebookPageURL",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GitHubURL",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LinkedInURL",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfilePictureURL",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "TemplateProjects");

            migrationBuilder.DropColumn(
                name: "Goals",
                table: "TemplateProjects");

            migrationBuilder.DropColumn(
                name: "ProjectPictureUrl",
                table: "TemplateProjects");

            migrationBuilder.DropColumn(
                name: "RequiredRoles",
                table: "TemplateProjects");

            migrationBuilder.DropColumn(
                name: "Technologies",
                table: "TemplateProjects");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Goals",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProgrammingLanguages",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectPictureUrl",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RequiredRoles",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Technologies",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "NonprofitExpertiseExpertiseId",
                table: "Nonprofits");

            migrationBuilder.DropColumn(
                name: "TechExpertiseExpertiseId",
                table: "Mentors");

            migrationBuilder.DropColumn(
                name: "TechExpertiseExpertiseId",
                table: "Gainers");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "TemplateProjects",
                newName: "TemplateProjectId");

            migrationBuilder.AlterColumn<int>(
                name: "DifficultyLevel",
                table: "Projects",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
