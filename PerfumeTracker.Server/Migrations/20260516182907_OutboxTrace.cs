using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class OutboxTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpanId",
                table: "OutboxMessage",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceFlags",
                table: "OutboxMessage",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "OutboxMessage",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpanId",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "TraceFlags",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "OutboxMessage");
        }
    }
}
