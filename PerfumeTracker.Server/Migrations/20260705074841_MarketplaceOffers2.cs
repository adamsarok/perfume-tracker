using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    /// <inheritdoc />
    public partial class MarketplaceOffers2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "MarketplaceOffer"
                ALTER COLUMN "SellerExternalId" TYPE integer
                USING NULLIF("SellerExternalId", '')::integer;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "MarketplaceOffer"
                ALTER COLUMN "SellerExternalId" TYPE text
                USING "SellerExternalId"::text;
                """);
        }
    }
}
