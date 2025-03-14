using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Perfume",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    House = table.Column<string>(type: "text", nullable: false),
                    PerfumeName = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false, defaultValueSql: "''::text"),
                    Ml = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    ImageObjectKey = table.Column<string>(type: "text", nullable: false, defaultValueSql: "''::text"),
                    Autumn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Spring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Summer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Winter = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FullText = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "PerfumeName", "House", "Notes", "Rating" }),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Perfume_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recommendation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Query = table.Column<string>(type: "text", nullable: false),
                    Recommendations = table.Column<string>(type: "text", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Recommendation_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Tag_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeSuggested",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerfumeId = table.Column<int>(type: "integer", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PerfumeSuggested_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PerfumeSuggested_perfumeId_fkey",
                        column: x => x.PerfumeId,
                        principalTable: "Perfume",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeWorn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerfumeId = table.Column<int>(type: "integer", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PerfumeWorn_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PerfumeWorn_perfumeId_fkey",
                        column: x => x.PerfumeId,
                        principalTable: "Perfume",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerfumeId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PerfumeTag_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PerfumeTag_perfumeId_fkey",
                        column: x => x.PerfumeId,
                        principalTable: "Perfume",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PerfumeTag_tagId_fkey",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Perfume_FullText",
                table: "Perfume",
                column: "FullText")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Perfume_House_PerfumeName",
                table: "Perfume",
                columns: new[] { "House", "PerfumeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeSuggested_PerfumeId",
                table: "PerfumeSuggested",
                column: "PerfumeId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeTag_PerfumeId",
                table: "PerfumeTag",
                column: "PerfumeId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeTag_TagId",
                table: "PerfumeTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeWorn_PerfumeId",
                table: "PerfumeWorn",
                column: "PerfumeId");

            migrationBuilder.CreateIndex(
                name: "Tag_tag_key",
                table: "Tag",
                column: "TagName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfumeSuggested");

            migrationBuilder.DropTable(
                name: "PerfumeTag");

            migrationBuilder.DropTable(
                name: "PerfumeWorn");

            migrationBuilder.DropTable(
                name: "Recommendation");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "Perfume");
        }
    }
}
