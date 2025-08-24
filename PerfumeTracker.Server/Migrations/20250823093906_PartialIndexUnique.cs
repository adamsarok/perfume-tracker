using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class PartialIndexUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Tag_tag_key",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Perfume_House_PerfumeName",
                table: "Perfume");

            migrationBuilder.CreateIndex(
                name: "Tag_tag_key",
                table: "Tag",
                columns: new[] { "UserId", "TagName" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Perfume_House_PerfumeName",
                table: "Perfume",
                columns: new[] { "House", "PerfumeName" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Tag_tag_key",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Perfume_House_PerfumeName",
                table: "Perfume");

            migrationBuilder.CreateIndex(
                name: "Tag_tag_key",
                table: "Tag",
                columns: new[] { "UserId", "TagName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Perfume_House_PerfumeName",
                table: "Perfume",
                columns: new[] { "House", "PerfumeName" },
                unique: true);
        }
    }
}
