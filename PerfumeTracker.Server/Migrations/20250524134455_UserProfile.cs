using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class UserProfile : Migration
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

            migrationBuilder.DropPrimaryKey(
                name: "UserProfile_pkey",
                table: "UserProfile");

            migrationBuilder.AddPrimaryKey(
                name: "UserProfile_pkey",
                table: "UserProfile",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "UserAchievement_UserId_fkey",
                table: "UserAchievement",
                column: "UserId",
                principalTable: "UserProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "UserMission_UserId_fkey",
                table: "UserMission",
                column: "UserId",
                principalTable: "UserProfile",
                principalColumn: "Id",
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
                name: "UserProfile_pkey",
                table: "UserProfile");

            migrationBuilder.AddPrimaryKey(
                name: "UserProfile_pkey",
                table: "UserProfile",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "UserAchievement_UserId_fkey",
                table: "UserAchievement",
                column: "UserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "UserMission_UserId_fkey",
                table: "UserMission",
                column: "UserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
