using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations {
	/// <inheritdoc />
	public partial class PersistLastWorn : Migration {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder) {
			migrationBuilder.AddColumn<DateTime>(
				name: "LastWorn",
				table: "Perfume",
				type: "timestamp with time zone",
				nullable: true);

			migrationBuilder.Sql(@"
				UPDATE ""Perfume"" p
				SET ""LastWorn"" = (SELECT MAX(pe.""EventDate"")
					FROM ""PerfumeEvent"" pe
					WHERE pe.""PerfumeId"" = p.""Id""
					AND pe.""IsDeleted"" = FALSE
					AND pe.""Type"" = 1)
			");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.DropColumn(
				name: "LastWorn",
				table: "Perfume");
		}
	}
}
