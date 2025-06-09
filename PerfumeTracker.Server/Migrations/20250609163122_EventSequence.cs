using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations {
	/// <inheritdoc />
	public partial class EventSequence : Migration {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder) {
			migrationBuilder.AddColumn<int>(
			name: "SequenceNumber",
			table: "PerfumeEvent",
			type: "integer",
			nullable: false,
			defaultValue: 0);

			migrationBuilder.CreateSequence(
				name: "PerfumeEventSequence",
				startValue: 1L,
				incrementBy: 1);

			migrationBuilder.Sql(@"
            WITH numbered_events AS (
                SELECT ""Id"", ROW_NUMBER() OVER (ORDER BY ""CreatedAt"", ""Id"") as row_num
                FROM ""PerfumeEvent""
            )
            UPDATE ""PerfumeEvent""
            SET ""SequenceNumber"" = numbered_events.row_num
            FROM numbered_events
            WHERE ""PerfumeEvent"".""Id"" = numbered_events.""Id"";
        ");

			migrationBuilder.AlterColumn<int>(
			name: "SequenceNumber",
				table: "PerfumeEvent",
				type: "integer",
				nullable: false,
				defaultValueSql: "nextval('\"PerfumeEventSequence\"')");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.DropSequence(
			name: "PerfumeEventSequence");

			migrationBuilder.DropColumn(
				name: "SequenceNumber",
				table: "PerfumeEvent");
		}
	}
}
