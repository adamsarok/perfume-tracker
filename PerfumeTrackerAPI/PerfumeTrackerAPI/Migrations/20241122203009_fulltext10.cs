using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace PerfumeTrackerAPI.Migrations
{
    /// <inheritdoc />
    public partial class fulltext10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "FullText",
                table: "Perfume",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "perfume", "house", "notes", "rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Perfume_FullText",
                table: "Perfume",
                column: "FullText")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Perfume_FullText",
                table: "Perfume");

            migrationBuilder.DropColumn(
                name: "FullText",
                table: "Perfume");
        }
    }
}
