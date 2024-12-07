using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTrackerAPI.Migrations
{
    /// <inheritdoc />
    public partial class dates2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Update_At",
                table: "PerfumeTag");

            migrationBuilder.RenameColumn(
                name: "Insert_At",
                table: "PerfumeTag",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "Update_At",
                table: "Perfume",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "Insert_At",
                table: "Perfume",
                newName: "Created_At");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "PerfumeTag",
                newName: "Insert_At");

            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "Perfume",
                newName: "Update_At");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Perfume",
                newName: "Insert_At");

            migrationBuilder.AddColumn<DateTime>(
                name: "Update_At",
                table: "PerfumeTag",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
