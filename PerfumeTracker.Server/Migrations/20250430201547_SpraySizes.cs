using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class SpraySizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SprayAmount",
                table: "Settings",
                newName: "SprayAmountSamplesMl");

            migrationBuilder.AddColumn<decimal>(
                name: "SprayAmountFullSizeMl",
                table: "Settings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SprayAmountFullSizeMl",
                table: "Settings");

            migrationBuilder.RenameColumn(
                name: "SprayAmountSamplesMl",
                table: "Settings",
                newName: "SprayAmount");
        }
    }
}
