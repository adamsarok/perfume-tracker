using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations {
	/// <inheritdoc />
	public partial class PersistWearCount : Migration {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder) {
			migrationBuilder.AddColumn<int>(
				name: "WearCount",
				table: "Perfume",
				type: "integer",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.Sql(@"
				UPDATE ""Perfume"" p
				SET ""WearCount"" = GREATEST(0, COALESCE(
					(SELECT COUNT(1)
					 FROM ""PerfumeEvent"" pe
					 WHERE pe.""PerfumeId"" = p.""Id""
					   AND pe.""IsDeleted"" = FALSE
					   AND pe.""Type"" = 1),
					0
				))
			");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.DropColumn(
				name: "WearCount",
				table: "Perfume");
		}
	}
}
