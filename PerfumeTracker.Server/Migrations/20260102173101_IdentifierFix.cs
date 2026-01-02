using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class IdentifierFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIdentifyBackfillFailed",
                table: "Perfume");

            migrationBuilder.AddColumn<int>(
                name: "PerfumeIdentifiedStatus",
                table: "Perfume",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerfumeIdentifiedStatus",
                table: "Perfume");

            migrationBuilder.AddColumn<bool>(
                name: "IsIdentifyBackfillFailed",
                table: "Perfume",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
