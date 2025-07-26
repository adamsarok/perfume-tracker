using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class Ratings3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
				INSERT INTO ""PerfumeRating"" (""Id"", ""UserId"", ""PerfumeId"", ""Rating"", ""Comment"", ""RatingDate"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
				SELECT 
					gen_random_uuid(), -- Generate new UUID for Id
					""UserId"",
					""Id"" as ""PerfumeId"",
					""Rating""::decimal as ""Rating"",
					""Notes"" as ""Comment"",
					CURRENT_TIMESTAMP as ""RatingDate"",
					CURRENT_TIMESTAMP as ""CreatedAt"",
					CURRENT_TIMESTAMP as ""UpdatedAt"",
					false as ""IsDeleted""
				FROM ""Perfume""
				WHERE ""Rating"" != 0 OR ""Notes"" IS NOT NULL;
			");

			migrationBuilder.DropIndex(
                name: "IX_Perfume_FullText",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "FullText",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Perfume");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumRating",
                table: "UserProfile",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "MinimumRating",
                table: "UserProfile",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "FullText",
                table: "Perfume",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "PerfumeName", "House", "Notes", "Rating" });

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Perfume",
                type: "text",
                nullable: false,
                defaultValueSql: "''::text");

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Perfume",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_Perfume_FullText",
                table: "Perfume",
                column: "FullText")
                .Annotation("Npgsql:IndexMethod", "GIN");

			migrationBuilder.Sql(@"
				UPDATE ""Perfume"" p
				SET 
					""Rating"" = subquery.""Rating"",
					""Notes"" = subquery.""Comment""
				FROM (
					SELECT DISTINCT ON (""PerfumeId"") 
						""PerfumeId"",
						""Rating""::double precision,
						""Comment""
					FROM ""PerfumeRating""
					ORDER BY ""PerfumeId"", ""RatingDate"" DESC
				) subquery
				WHERE p.""Id"" = subquery.""PerfumeId""
			");
		}
    }
}
