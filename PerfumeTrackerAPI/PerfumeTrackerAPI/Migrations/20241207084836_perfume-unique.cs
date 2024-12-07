using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTrackerAPI.Migrations
{
    /// <inheritdoc />
    public partial class perfumeunique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Perfume_house_perfume",
                table: "Perfume",
                columns: new[] { "house", "perfume" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Perfume_house_perfume",
                table: "Perfume");
        }
    }
}
