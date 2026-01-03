using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class Agent2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToolArguments",
                table: "ChatMessage");

            migrationBuilder.AddColumn<int>(
                name: "ChatFinishReason",
                table: "ChatMessage",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatFinishReason",
                table: "ChatMessage");

            migrationBuilder.AddColumn<string>(
                name: "ToolArguments",
                table: "ChatMessage",
                type: "text",
                nullable: true);
        }
    }
}
