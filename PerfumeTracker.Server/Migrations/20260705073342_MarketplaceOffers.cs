using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketplaceOffer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    SourceOfferId = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    SellerName = table.Column<string>(type: "text", nullable: true),
                    SellerExternalId = table.Column<string>(type: "text", nullable: true),
                    BrandName = table.Column<string>(type: "text", nullable: true),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    OfferTitle = table.Column<string>(type: "text", nullable: false),
                    PriceText = table.Column<string>(type: "text", nullable: true),
                    SizeText = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ListedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchedPerfumeId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchConfidence = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("MarketplaceOffer_pkey", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOffer_UserId_Source_SourceOfferId",
                table: "MarketplaceOffer",
                columns: new[] { "UserId", "Source", "SourceOfferId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOffer_UserId_Status_MatchConfidence",
                table: "MarketplaceOffer",
                columns: new[] { "UserId", "Status", "MatchConfidence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceOffer");
        }
    }
}
