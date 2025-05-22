using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class Missions2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "PerfumeSuggested",
                newName: "PerfumeRandom");

            migrationBuilder.RenameIndex(
                name: "IX_PerfumeSuggested_PerfumeId",
                table: "PerfumeRandom",
                newName: "IX_PerfumeRandom_PerfumeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "PerfumeRandom",
                newName: "PerfumeSuggested");

            migrationBuilder.RenameIndex(
                name: "IX_PerfumeRandom_PerfumeId",
                table: "PerfumeSuggested",
                newName: "IX_PerfumeSuggested_PerfumeId");
        }
    }
}
