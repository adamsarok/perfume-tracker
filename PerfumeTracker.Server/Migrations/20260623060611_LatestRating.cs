using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class LatestRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AverageRating",
                table: "Perfume",
                newName: "LatestRating");

            migrationBuilder.Sql(@"
                UPDATE ""Perfume"" p
                SET ""LatestRating"" = COALESCE((SELECT pr.""Rating""
                    FROM ""PerfumeRating"" pr
                    WHERE pr.""PerfumeId"" = p.""Id""
                    AND pr.""IsDeleted"" = FALSE
                    ORDER BY pr.""RatingDate"" DESC, pr.""CreatedAt"" DESC
                    LIMIT 1), 0)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LatestRating",
                table: "Perfume",
                newName: "AverageRating");
        }
    }
}
