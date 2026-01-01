using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations {
	/// <inheritdoc />
	public partial class PersistMlLeft : Migration {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder) {
			migrationBuilder.AddColumn<decimal>(
				name: "MlLeft",
				table: "Perfume",
				type: "numeric",
				nullable: false,
				defaultValue: 0m);

			migrationBuilder.Sql(@"
				UPDATE ""Perfume"" p
				SET ""MlLeft"" = GREATEST(0, COALESCE(
					(SELECT SUM(pe.""AmountMl"")
					 FROM ""PerfumeEvent"" pe
					 WHERE pe.""PerfumeId"" = p.""Id""
					   AND pe.""IsDeleted"" = FALSE),
					0
				))
			");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.DropColumn(
				name: "MlLeft",
				table: "Perfume");
		}
	}
}
