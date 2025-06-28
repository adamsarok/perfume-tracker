using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class ImageGuid2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"update ""Perfume""
				set ""ImageObjectKeyNew"" = CAST(""ImageObjectKey"" AS uuid)
				where ""ImageObjectKey"" != '';");
            migrationBuilder.DropColumn(
                name: "ImageObjectKey",
                table: "Perfume");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageObjectKey",
                table: "Perfume",
                type: "text",
                nullable: false,
                defaultValueSql: "''::text");
        }
    }
}
