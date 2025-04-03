using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTeamMembersRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamMemberIds",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "OwningOrganizationId",
                table: "Projects",
                newName: "OwningOrganizationUserId");

            migrationBuilder.RenameColumn(
                name: "AssignedMentorId",
                table: "Projects",
                newName: "AssignedMentorUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProjectId",
                table: "Users",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_AssignedMentorUserId",
                table: "Projects",
                column: "AssignedMentorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwningOrganizationUserId",
                table: "Projects",
                column: "OwningOrganizationUserId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropIndex(
                name: "IX_Projects_AssignedMentorUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwningOrganizationUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "OwningOrganizationUserId",
                table: "Projects",
                newName: "OwningOrganizationId");

            migrationBuilder.RenameColumn(
                name: "AssignedMentorUserId",
                table: "Projects",
                newName: "AssignedMentorId");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "TeamMemberIds",
                table: "Projects",
                type: "uuid[]",
                nullable: false);
        }
    }
}
