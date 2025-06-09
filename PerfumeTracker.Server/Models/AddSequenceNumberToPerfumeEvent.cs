//using Microsoft.EntityFrameworkCore.Migrations;

//namespace PerfumeTracker.Server.Migrations;

//public partial class AddSequenceNumberToPerfumeEvent : Migration
//{
//    protected override void Up(MigrationBuilder migrationBuilder)
//    {
//        migrationBuilder.AddColumn<int>(
//            name: "SequenceNumber",
//            table: "PerfumeEvents",
//            type: "integer",
//            nullable: false,
//            defaultValue: 0);

//        // Create a sequence for the SequenceNumber
//        migrationBuilder.CreateSequence(
//            name: "PerfumeEventSequence",
//            startValue: 1L,
//            incrementBy: 1);

//        // Update existing records with sequential numbers
//        migrationBuilder.Sql(@"
//            WITH numbered_events AS (
//                SELECT Id, ROW_NUMBER() OVER (ORDER BY CreatedAt, Id) as row_num
//                FROM ""PerfumeEvents""
//            )
//            UPDATE ""PerfumeEvents""
//            SET ""SequenceNumber"" = numbered_events.row_num
//            FROM numbered_events
//            WHERE ""PerfumeEvents"".Id = numbered_events.Id;
//        ");

//        // Set up the default value to use the sequence
//        migrationBuilder.AlterColumn<int>(
//            name: "SequenceNumber",
//            table: "PerfumeEvents",
//            type: "integer",
//            nullable: false,
//            defaultValueSql: "nextval('\"PerfumeEventSequence\"')");
//    }

//    protected override void Down(MigrationBuilder migrationBuilder)
//    {
//        migrationBuilder.DropSequence(
//            name: "PerfumeEventSequence");

//        migrationBuilder.DropColumn(
//            name: "SequenceNumber",
//            table: "PerfumeEvents");
//    }
//} 