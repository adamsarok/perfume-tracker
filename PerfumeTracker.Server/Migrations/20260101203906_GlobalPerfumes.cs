using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class GlobalPerfumes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalPerfume",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    House = table.Column<string>(type: "text", nullable: false),
                    PerfumeName = table.Column<string>(type: "text", nullable: false),
                    Family = table.Column<string>(type: "text", nullable: false),
                    Ml = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 2m),
                    MlLeft = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    WearCount = table.Column<int>(type: "integer", nullable: false),
                    ImageObjectKeyNew = table.Column<Guid>(type: "uuid", nullable: true),
                    Autumn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Spring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Summer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Winter = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FullText = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "simple")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "PerfumeName", "House" }),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("GlobalPerfume_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalTag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TagName = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("GlobalTag_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalPerfumeTag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfumeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("GlobalPerfumeTag_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "GlobalPerfumeTag_perfumeId_fkey",
                        column: x => x.PerfumeId,
                        principalTable: "GlobalPerfume",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "GlobalPerfumeTag_tagId_fkey",
                        column: x => x.TagId,
                        principalTable: "GlobalTag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalPerfume_FullText",
                table: "GlobalPerfume",
                column: "FullText")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalPerfume_House_PerfumeName",
                table: "GlobalPerfume",
                columns: new[] { "House", "PerfumeName" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalPerfumeTag_PerfumeId",
                table: "GlobalPerfumeTag",
                column: "PerfumeId");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalPerfumeTag_TagId",
                table: "GlobalPerfumeTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "Global_tag_key",
                table: "GlobalTag",
                column: "TagName",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalPerfumeTag");

            migrationBuilder.DropTable(
                name: "GlobalPerfume");

            migrationBuilder.DropTable(
                name: "GlobalTag");
        }
    }
}
