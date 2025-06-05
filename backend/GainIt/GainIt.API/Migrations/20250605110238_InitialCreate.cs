using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "TemplateProjects",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProjectDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: false),
                    ProjectPictureUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Goals = table.Column<List<string>>(type: "text[]", maxLength: 2000, nullable: false),
                    Technologies = table.Column<List<string>>(type: "text[]", nullable: false),
                    RequiredRoles = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateProjects", x => x.ProjectId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserRole = table.Column<int>(type: "integer", nullable: false),
                    Biography = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FacebookPageURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkedInURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GitHubURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProfilePictureURL = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
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

            migrationBuilder.CreateTable(
                name: "Nonprofits",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: false),
                    NonprofitExpertiseExpertiseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nonprofits", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Nonprofits_NonprofitExpertises_NonprofitExpertiseExpertiseId",
                        column: x => x.NonprofitExpertiseExpertiseId,
                        principalTable: "NonprofitExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nonprofits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Gainers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EducationStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AreasOfInterest = table.Column<List<string>>(type: "text[]", nullable: false),
                    TechExpertiseExpertiseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gainers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Gainers_TechExpertises_TechExpertiseExpertiseId",
                        column: x => x.TechExpertiseExpertiseId,
                        principalTable: "TechExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Gainers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mentors",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false),
                    AreaOfExpertise = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TechExpertiseExpertiseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mentors", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Mentors_TechExpertises_TechExpertiseExpertiseId",
                        column: x => x.TechExpertiseExpertiseId,
                        principalTable: "TechExpertises",
                        principalColumn: "ExpertiseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mentors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProjectDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: false),
                    ProjectPictureUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Goals = table.Column<List<string>>(type: "text[]", maxLength: 2000, nullable: false),
                    Technologies = table.Column<List<string>>(type: "text[]", nullable: false),
                    RequiredRoles = table.Column<List<string>>(type: "text[]", nullable: false),
                    ProjectStatus = table.Column<int>(type: "integer", nullable: false),
                    ProjectSource = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RepositoryLink = table.Column<string>(type: "text", nullable: true),
                    AssignedMentorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwningOrganizationUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProgrammingLanguages = table.Column<List<string>>(type: "text[]", nullable: false),
                    GainerUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_Gainers_GainerUserId",
                        column: x => x.GainerUserId,
                        principalTable: "Gainers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Projects_Mentors_AssignedMentorUserId",
                        column: x => x.AssignedMentorUserId,
                        principalTable: "Mentors",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Projects_Nonprofits_OwningOrganizationUserId",
                        column: x => x.OwningOrganizationUserId,
                        principalTable: "Nonprofits",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateIndex(
                name: "IX_Gainers_TechExpertiseExpertiseId",
                table: "Gainers",
                column: "TechExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_Mentors_TechExpertiseExpertiseId",
                table: "Mentors",
                column: "TechExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_Nonprofits_NonprofitExpertiseExpertiseId",
                table: "Nonprofits",
                column: "NonprofitExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_AssignedMentorUserId",
                table: "Projects",
                column: "AssignedMentorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_GainerUserId",
                table: "Projects",
                column: "GainerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwningOrganizationUserId",
                table: "Projects",
                column: "OwningOrganizationUserId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "TemplateProjects");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "AchievementTemplates");

            migrationBuilder.DropTable(
                name: "Gainers");

            migrationBuilder.DropTable(
                name: "Mentors");

            migrationBuilder.DropTable(
                name: "Nonprofits");

            migrationBuilder.DropTable(
                name: "TechExpertises");

            migrationBuilder.DropTable(
                name: "NonprofitExpertises");

            migrationBuilder.DropTable(
                name: "UserExpertises");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
