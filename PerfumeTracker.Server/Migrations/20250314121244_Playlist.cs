using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class Playlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerfumePlayList",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PerfumePlayList_pkey", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "PerfumePerfumePlayList",
                columns: table => new
                {
                    PerfumePlayListName = table.Column<string>(type: "text", nullable: false),
                    PerfumesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfumePerfumePlayList", x => new { x.PerfumePlayListName, x.PerfumesId });
                    table.ForeignKey(
                        name: "FK_PerfumePerfumePlayList_PerfumePlayList_PerfumePlayListName",
                        column: x => x.PerfumePlayListName,
                        principalTable: "PerfumePlayList",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerfumePerfumePlayList_Perfume_PerfumesId",
                        column: x => x.PerfumesId,
                        principalTable: "Perfume",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerfumePerfumePlayList_PerfumesId",
                table: "PerfumePerfumePlayList",
                column: "PerfumesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfumePerfumePlayList");

            migrationBuilder.DropTable(
                name: "PerfumePlayList");
        }
    }
}
