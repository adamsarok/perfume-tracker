using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class PerfumeCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Autumn",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "Spring",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "Summer",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "Winter",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "Autumn",
                table: "GlobalPerfume");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "GlobalPerfume");

            migrationBuilder.DropColumn(
                name: "Ml",
                table: "GlobalPerfume");

            migrationBuilder.DropColumn(
                name: "MlLeft",
                table: "GlobalPerfume");

            migrationBuilder.DropColumn(
                name: "Spring",
                table: "GlobalPerfume");

            migrationBuilder.DropColumn(
                name: "Summer",
                table: "GlobalPerfume");

            migrationBuilder.DropColumn(
                name: "WearCount",
                table: "GlobalPerfume");

            migrationBuilder.DropColumn(
                name: "Winter",
                table: "GlobalPerfume");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Autumn",
                table: "Perfume",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Spring",
                table: "Perfume",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Summer",
                table: "Perfume",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Winter",
                table: "Perfume",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Autumn",
                table: "GlobalPerfume",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "GlobalPerfume",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Ml",
                table: "GlobalPerfume",
                type: "numeric",
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.AddColumn<decimal>(
                name: "MlLeft",
                table: "GlobalPerfume",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Spring",
                table: "GlobalPerfume",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Summer",
                table: "GlobalPerfume",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "WearCount",
                table: "GlobalPerfume",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Winter",
                table: "GlobalPerfume",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
