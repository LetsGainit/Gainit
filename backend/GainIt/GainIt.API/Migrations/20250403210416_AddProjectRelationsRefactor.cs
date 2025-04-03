using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectRelationsRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_AssignedMentorUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwningOrganizationUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Projects_ProjectId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ProjectId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "ProjectTeamMembers",
                columns: table => new
                {
                    ParticipatedProjectsProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamMembersUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeamMembers", x => new { x.ParticipatedProjectsProjectId, x.TeamMembersUserId });
                    table.ForeignKey(
                        name: "FK_ProjectTeamMembers_Gainers_TeamMembersUserId",
                        column: x => x.TeamMembersUserId,
                        principalTable: "Gainers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeamMembers_Projects_ParticipatedProjectsProjectId",
                        column: x => x.ParticipatedProjectsProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeamMembers_TeamMembersUserId",
                table: "ProjectTeamMembers",
                column: "TeamMembersUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Mentors_AssignedMentorUserId",
                table: "Projects",
                column: "AssignedMentorUserId",
                principalTable: "Mentors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Nonprofits_OwningOrganizationUserId",
                table: "Projects",
                column: "OwningOrganizationUserId",
                principalTable: "Nonprofits",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Mentors_AssignedMentorUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Nonprofits_OwningOrganizationUserId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectTeamMembers");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProjectId",
                table: "Users",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_AssignedMentorUserId",
                table: "Projects",
                column: "AssignedMentorUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_OwningOrganizationUserId",
                table: "Projects",
                column: "OwningOrganizationUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Projects_ProjectId",
                table: "Users",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId");
        }
    }
}
