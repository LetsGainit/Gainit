using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantTechExpertiseFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropIndex(
                name: "IX_Mentors_TechExpertiseExpertiseId",
                table: "Mentors");

            migrationBuilder.DropIndex(
                name: "IX_Gainers_TechExpertiseExpertiseId",
                table: "Gainers");

            migrationBuilder.DropColumn(
                name: "TechExpertiseExpertiseId",
                table: "Mentors");

            migrationBuilder.DropColumn(
                name: "TechExpertiseExpertiseId",
                table: "Gainers");

            migrationBuilder.AlterColumn<Guid>(
                name: "NonprofitExpertiseExpertiseId",
                table: "Nonprofits",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Nonprofits_NonprofitExpertises_NonprofitExpertiseExpertiseId",
                table: "Nonprofits",
                column: "NonprofitExpertiseExpertiseId",
                principalTable: "NonprofitExpertises",
                principalColumn: "ExpertiseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nonprofits_NonprofitExpertises_NonprofitExpertiseExpertiseId",
                table: "Nonprofits");

            migrationBuilder.AlterColumn<Guid>(
                name: "NonprofitExpertiseExpertiseId",
                table: "Nonprofits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Mentors_TechExpertiseExpertiseId",
                table: "Mentors",
                column: "TechExpertiseExpertiseId");

            migrationBuilder.CreateIndex(
                name: "IX_Gainers_TechExpertiseExpertiseId",
                table: "Gainers",
                column: "TechExpertiseExpertiseId");

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
    }
}
