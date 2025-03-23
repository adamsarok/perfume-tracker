using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class MlLeft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Ml",
                table: "Perfume",
                type: "numeric",
                nullable: false,
                defaultValue: 2m,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "MlLeft",
                table: "Perfume",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MlLeft",
                table: "Perfume");

            migrationBuilder.AlterColumn<int>(
                name: "Ml",
                table: "Perfume",
                type: "integer",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 2m);
        }
    }
}
