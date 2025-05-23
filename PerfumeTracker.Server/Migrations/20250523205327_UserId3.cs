using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class UserId3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserProfileNew");

            migrationBuilder.AddPrimaryKey(
                name: "UserProfileNew_pkey",
                table: "UserProfileNew",
                column: "UserId");

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

            migrationBuilder.DropPrimaryKey(
                name: "UserProfileNew_pkey",
                table: "UserProfileNew");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "UserProfileNew",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserProfileNew",
                table: "UserProfileNew",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "UserAchievement_UserId_fkey",
                table: "UserAchievement",
                column: "UserId",
                principalTable: "UserProfileNew",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "UserMission_UserId_fkey",
                table: "UserMission",
                column: "UserId",
                principalTable: "UserProfileNew",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
