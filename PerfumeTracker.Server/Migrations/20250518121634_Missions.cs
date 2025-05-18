using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class Missions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "PerfumeSuggested_perfumeId_fkey",
                table: "PerfumeSuggested");

            migrationBuilder.DropPrimaryKey(
                name: "PerfumeSuggested_pkey",
                table: "PerfumeSuggested");

            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "PerfumeSuggested",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PerfumeRandom_pkey",
                table: "PerfumeSuggested",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Mission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    XP = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    RequiredCount = table.Column<int>(type: "integer", nullable: true),
                    RequiredId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Mission_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserMission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MissionId = table.Column<int>(type: "integer", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("UserMission_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "UserMission_MissionId_fkey",
                        column: x => x.MissionId,
                        principalTable: "Mission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "UserMission_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMission_MissionId",
                table: "UserMission",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMission_UserId",
                table: "UserMission",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "PerfumeRandom_perfumeId_fkey",
                table: "PerfumeSuggested",
                column: "PerfumeId",
                principalTable: "Perfume",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "PerfumeRandom_perfumeId_fkey",
                table: "PerfumeSuggested");

            migrationBuilder.DropTable(
                name: "UserMission");

            migrationBuilder.DropTable(
                name: "Mission");

            migrationBuilder.DropPrimaryKey(
                name: "PerfumeRandom_pkey",
                table: "PerfumeSuggested");

            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "PerfumeSuggested");

            migrationBuilder.AddPrimaryKey(
                name: "PerfumeSuggested_pkey",
                table: "PerfumeSuggested",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "PerfumeSuggested_perfumeId_fkey",
                table: "PerfumeSuggested",
                column: "PerfumeId",
                principalTable: "Perfume",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
