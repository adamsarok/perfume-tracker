using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations {
	/// <inheritdoc />
	public partial class PersistAverageRating : Migration {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder) {
			migrationBuilder.AddColumn<decimal>(
				name: "AverageRating",
				table: "Perfume",
				type: "numeric",
				nullable: false,
				defaultValue: 0m);

			migrationBuilder.Sql(@"
				UPDATE ""Perfume"" p
				SET ""AverageRating"" = GREATEST(0, COALESCE(
					(SELECT AVG(pr.""Rating"")
					 FROM ""PerfumeRating"" pr
					 WHERE pr.""PerfumeId"" = p.""Id""
					   AND pr.""IsDeleted"" = FALSE),
					0
				))
			");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.DropColumn(
				name: "AverageRating",
				table: "Perfume");
		}
	}
}
