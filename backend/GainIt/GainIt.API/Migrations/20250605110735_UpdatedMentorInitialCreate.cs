using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedMentorInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Mentors_AssignedMentorUserId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "AssignedMentorUserId",
                table: "Projects",
                newName: "MentorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_AssignedMentorUserId",
                table: "Projects",
                newName: "IX_Projects_MentorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Mentors_MentorUserId",
                table: "Projects",
                column: "MentorUserId",
                principalTable: "Mentors",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Mentors_MentorUserId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "MentorUserId",
                table: "Projects",
                newName: "AssignedMentorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_MentorUserId",
                table: "Projects",
                newName: "IX_Projects_AssignedMentorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Mentors_AssignedMentorUserId",
                table: "Projects",
                column: "AssignedMentorUserId",
                principalTable: "Mentors",
                principalColumn: "UserId");
        }
    }
}
