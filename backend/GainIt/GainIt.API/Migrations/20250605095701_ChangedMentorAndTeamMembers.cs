using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class ChangedMentorAndTeamMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Mentors_AssignedMentorUserId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectTeamMembers");

            migrationBuilder.AddColumn<Guid>(
                name: "GainerUserId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_GainerUserId",
                table: "Projects",
                column: "GainerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Gainers_GainerUserId",
                table: "Projects",
                column: "GainerUserId",
                principalTable: "Gainers",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Mentors_AssignedMentorUserId",
                table: "Projects",
                column: "AssignedMentorUserId",
                principalTable: "Mentors",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Gainers_GainerUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Mentors_AssignedMentorUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_GainerUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "GainerUserId",
                table: "Projects");

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
        }
    }
}
