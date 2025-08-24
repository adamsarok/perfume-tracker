using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class PartialIndexUnique2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Perfume_House_PerfumeName",
                table: "Perfume");

            migrationBuilder.CreateIndex(
                name: "IX_Perfume_UserId_House_PerfumeName",
                table: "Perfume",
                columns: new[] { "UserId", "House", "PerfumeName" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Perfume_UserId_House_PerfumeName",
                table: "Perfume");

            migrationBuilder.CreateIndex(
                name: "IX_Perfume_House_PerfumeName",
                table: "Perfume",
                columns: new[] { "House", "PerfumeName" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
