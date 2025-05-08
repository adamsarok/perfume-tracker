using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class PerfumeEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "PerfumeWorn_perfumeId_fkey",
                table: "PerfumeWorn");

            migrationBuilder.DropPrimaryKey(
                name: "PerfumeWorn_pkey",
                table: "PerfumeWorn");

            migrationBuilder.RenameTable(
                name: "PerfumeWorn",
                newName: "PerfumeEvent");

            migrationBuilder.RenameColumn(
                name: "WornOn",
                table: "PerfumeEvent",
                newName: "EventDate");

            migrationBuilder.RenameIndex(
                name: "IX_PerfumeWorn_PerfumeId",
                table: "PerfumeEvent",
                newName: "IX_PerfumeEvent_PerfumeId");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountMl",
                table: "PerfumeEvent",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "PerfumeEvent",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PerfumeEvent_pkey",
                table: "PerfumeEvent",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "PerfumeEvent_perfumeId_fkey",
                table: "PerfumeEvent",
                column: "PerfumeId",
                principalTable: "Perfume",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "PerfumeEvent_perfumeId_fkey",
                table: "PerfumeEvent");

            migrationBuilder.DropPrimaryKey(
                name: "PerfumeEvent_pkey",
                table: "PerfumeEvent");

            migrationBuilder.DropColumn(
                name: "AmountMl",
                table: "PerfumeEvent");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PerfumeEvent");

            migrationBuilder.RenameTable(
                name: "PerfumeEvent",
                newName: "PerfumeWorn");

            migrationBuilder.RenameColumn(
                name: "EventDate",
                table: "PerfumeWorn",
                newName: "WornOn");

            migrationBuilder.RenameIndex(
                name: "IX_PerfumeEvent_PerfumeId",
                table: "PerfumeWorn",
                newName: "IX_PerfumeWorn_PerfumeId");

            migrationBuilder.AddPrimaryKey(
                name: "PerfumeWorn_pkey",
                table: "PerfumeWorn",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "PerfumeWorn_perfumeId_fkey",
                table: "PerfumeWorn",
                column: "PerfumeId",
                principalTable: "Perfume",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
