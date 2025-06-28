using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class ImageGuid1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Tag_tag_key",
                table: "Tag");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageObjectKeyNew",
                table: "Perfume",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "Tag_tag_key",
                table: "Tag",
                columns: new[] { "UserId", "TagName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Tag_tag_key",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "ImageObjectKeyNew",
                table: "Perfume");

            migrationBuilder.CreateIndex(
                name: "Tag_tag_key",
                table: "Tag",
                column: "TagName",
                unique: true);
        }
    }
}
