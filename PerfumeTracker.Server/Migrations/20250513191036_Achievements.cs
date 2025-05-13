using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class Achievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    MinPerfumesAdded = table.Column<int>(type: "integer", nullable: true),
                    MinPerfumeWorns = table.Column<int>(type: "integer", nullable: true),
                    MinTags = table.Column<int>(type: "integer", nullable: true),
                    MinPerfumeTags = table.Column<int>(type: "integer", nullable: true),
                    MinStreak = table.Column<int>(type: "integer", nullable: true),
                    MinXP = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Achievement_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    XP = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MinimumRating = table.Column<double>(type: "double precision", nullable: false),
                    DayFilter = table.Column<int>(type: "integer", nullable: false),
                    ShowMalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowUnisexPerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowFemalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    SprayAmountFullSizeMl = table.Column<decimal>(type: "numeric", nullable: false),
                    SprayAmountSamplesMl = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AchievementId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("UserAchievement_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "UserAchievement_AchievementId_fkey",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "UserAchievement_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievement_AchievementId",
                table: "UserAchievement",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievement_UserId",
                table: "UserAchievement",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAchievement");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DayFilter = table.Column<int>(type: "integer", nullable: false),
                    MinimumRating = table.Column<double>(type: "double precision", nullable: false),
                    ShowFemalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowMalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowUnisexPerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    SprayAmountFullSizeMl = table.Column<decimal>(type: "numeric", nullable: false),
                    SprayAmountSamplesMl = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Settings_pkey", x => x.UserId);
                });
        }
    }
}
