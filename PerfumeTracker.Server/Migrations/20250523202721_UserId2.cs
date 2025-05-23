using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class UserId2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "UserAchievement_UserId_fkey",
                table: "UserAchievement");

            migrationBuilder.DropForeignKey(
                name: "UserMission_UserId_fkey",
                table: "UserMission");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserMission",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserAchievement",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Tag",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Recommendation",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PerfumeTag",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PerfumeRandom",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PerfumePlayList",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PerfumeEvent",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Perfume",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "OutboxMessage",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Mission",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Achievement",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserProfileNew",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    XP = table.Column<int>(type: "integer", nullable: false),
                    MinimumRating = table.Column<double>(type: "double precision", nullable: false),
                    DayFilter = table.Column<int>(type: "integer", nullable: false),
                    ShowMalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowUnisexPerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowFemalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    SprayAmountFullSizeMl = table.Column<decimal>(type: "numeric", nullable: false),
                    SprayAmountSamplesMl = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileNew", x => x.Id);
                });

			migrationBuilder.AddForeignKey(
				name: "UserAchievement_UserId_fkey",
				table: "UserAchievement",
				column: "UserId",
				principalTable: "UserProfileNew",
				principalColumn: "UserId",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "UserMission_UserId_fkey",
				table: "UserMission",
				column: "UserId",
				principalTable: "UserProfileNew",
				principalColumn: "UserId",
				onDelete: ReferentialAction.Cascade);
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "UserAchievement_UserId_fkey",
                table: "UserAchievement");

            migrationBuilder.DropForeignKey(
                name: "UserMission_UserId_fkey",
                table: "UserMission");

            migrationBuilder.DropTable(
                name: "UserProfileNew");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Recommendation");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PerfumeTag");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PerfumeRandom");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PerfumePlayList");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PerfumeEvent");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Mission");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Achievement");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserMission",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserAchievement",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DayFilter = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumRating = table.Column<double>(type: "double precision", nullable: false),
                    ShowFemalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowMalePerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    ShowUnisexPerfumes = table.Column<bool>(type: "boolean", nullable: false),
                    SprayAmountFullSizeMl = table.Column<decimal>(type: "numeric", nullable: false),
                    SprayAmountSamplesMl = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    XP = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

			migrationBuilder.AddForeignKey(
				name: "UserAchievement_UserId_fkey",
				table: "UserAchievement",
				column: "UserId",
				principalTable: "UserProfiles",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "UserMission_UserId_fkey",
				table: "UserMission",
				column: "UserId",
				principalTable: "UserProfiles",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}
    }
}
