using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class WornOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "Tag",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Tag",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "Settings",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Settings",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "Recommendation",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Recommendation",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "PerfumeWorn",
                newName: "WornOn");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "PerfumeWorn",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "PerfumeTag",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "PerfumeTag",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "PerfumeSuggested",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "PerfumeSuggested",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "PerfumePlayList",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "PerfumePlayList",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Updated_At",
                table: "Perfume",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Perfume",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PerfumeWorn",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PerfumeWorn");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Tag",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Tag",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Settings",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Settings",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Recommendation",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Recommendation",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "WornOn",
                table: "PerfumeWorn",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PerfumeWorn",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PerfumeTag",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PerfumeTag",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PerfumeSuggested",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PerfumeSuggested",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PerfumePlayList",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PerfumePlayList",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Perfume",
                newName: "Updated_At");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Perfume",
                newName: "Created_At");
        }
    }
}
