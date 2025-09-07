using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GainIt.API.Migrations
{
    /// <inheritdoc />
    public partial class FixDurationColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Projects"" 
                ALTER COLUMN ""Duration"" TYPE integer 
                USING EXTRACT(EPOCH FROM ""Duration"") / 86400;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Projects"" 
                ALTER COLUMN ""Duration"" TYPE interval 
                USING (""Duration"" * 86400)::interval;
            ");
        }
    }
}
