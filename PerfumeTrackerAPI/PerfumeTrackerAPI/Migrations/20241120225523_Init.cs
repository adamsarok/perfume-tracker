using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PerfumeTrackerAPI.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgagent.pgagent", ",,");

            migrationBuilder.CreateTable(
                name: "_prisma_migrations",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    migration_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    logs = table.Column<string>(type: "text", nullable: true),
                    rolled_back_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    applied_steps_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("_prisma_migrations_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Perfume",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    house = table.Column<string>(type: "text", nullable: false),
                    perfume = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<double>(type: "double precision", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false, defaultValueSql: "''::text"),
                    ml = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    imageObjectKey = table.Column<string>(type: "text", nullable: false, defaultValueSql: "''::text"),
                    autumn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    spring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    summer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    winter = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Perfume_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Recommendation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    query = table.Column<string>(type: "text", nullable: false),
                    recommendations = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp(3) without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Recommendation_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tag = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Tag_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeSuggested",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    perfumeId = table.Column<int>(type: "integer", nullable: false),
                    suggestedOn = table.Column<DateTime>(type: "timestamp(3) without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PerfumeSuggested_pkey", x => x.id);
                    table.ForeignKey(
                        name: "PerfumeSuggested_perfumeId_fkey",
                        column: x => x.perfumeId,
                        principalTable: "Perfume",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeWorn",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    perfumeId = table.Column<int>(type: "integer", nullable: false),
                    wornOn = table.Column<DateTime>(type: "timestamp(3) without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PerfumeWorn_pkey", x => x.id);
                    table.ForeignKey(
                        name: "PerfumeWorn_perfumeId_fkey",
                        column: x => x.perfumeId,
                        principalTable: "Perfume",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeTag",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    perfumeId = table.Column<int>(type: "integer", nullable: false),
                    tagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PerfumeTag_pkey", x => x.id);
                    table.ForeignKey(
                        name: "PerfumeTag_perfumeId_fkey",
                        column: x => x.perfumeId,
                        principalTable: "Perfume",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PerfumeTag_tagId_fkey",
                        column: x => x.tagId,
                        principalTable: "Tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeSuggested_perfumeId",
                table: "PerfumeSuggested",
                column: "perfumeId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeTag_perfumeId",
                table: "PerfumeTag",
                column: "perfumeId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeTag_tagId",
                table: "PerfumeTag",
                column: "tagId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeWorn_perfumeId",
                table: "PerfumeWorn",
                column: "perfumeId");

            migrationBuilder.CreateIndex(
                name: "Tag_tag_key",
                table: "Tag",
                column: "tag",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_prisma_migrations");

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
