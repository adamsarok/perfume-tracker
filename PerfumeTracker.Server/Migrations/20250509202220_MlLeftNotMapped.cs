using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class MlLeftNotMapped : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MlLeft",
                table: "Perfume");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MlLeft",
                table: "Perfume",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
