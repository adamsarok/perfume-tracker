using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCachedCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "PerfumeRecommendation_CompletionId_fkey",
                table: "PerfumeRecommendation");

            migrationBuilder.DropTable(
                name: "CachedCompletion");

            migrationBuilder.DropIndex(
                name: "IX_PerfumeRecommendation_CompletionId",
                table: "PerfumeRecommendation");

            migrationBuilder.DropColumn(
                name: "CompletionId",
                table: "PerfumeRecommendation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompletionId",
                table: "PerfumeRecommendation",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CachedCompletion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletionType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Response = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("CachedCompletion_pkey", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeRecommendation_CompletionId",
                table: "PerfumeRecommendation",
                column: "CompletionId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedCompletion_UserId_Type_Prompt",
                table: "CachedCompletion",
                columns: new[] { "UserId", "CompletionType", "Prompt" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "PerfumeRecommendation_CompletionId_fkey",
                table: "PerfumeRecommendation",
                column: "CompletionId",
                principalTable: "CachedCompletion",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
