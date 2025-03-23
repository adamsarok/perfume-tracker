using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class Settings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    MinimumRating = table.Column<float>(type: "real", nullable: false),
                    DayFilter = table.Column<int>(type: "integer", nullable: false),
                    ShowMalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowUnisexPerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowFemalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    SprayAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Settings_pkey", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
