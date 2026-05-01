using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class NoteGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NoteGroup",
                table: "Tag",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoteGroup",
                table: "Tag");
        }
    }
}
