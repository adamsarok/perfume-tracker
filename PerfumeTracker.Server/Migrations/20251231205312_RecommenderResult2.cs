using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class RecommenderResult2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "PerfumeRecommendation_CompletionId_fkey",
                table: "PerfumeRecommendation");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompletionId",
                table: "PerfumeRecommendation",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "PerfumeRecommendation_CompletionId_fkey",
                table: "PerfumeRecommendation",
                column: "CompletionId",
                principalTable: "CachedCompletion",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "PerfumeRecommendation_CompletionId_fkey",
                table: "PerfumeRecommendation");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompletionId",
                table: "PerfumeRecommendation",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "PerfumeRecommendation_CompletionId_fkey",
                table: "PerfumeRecommendation",
                column: "CompletionId",
                principalTable: "CachedCompletion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
