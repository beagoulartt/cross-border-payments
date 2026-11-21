using CrossBorderPayments.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrossBorderPayments.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261121120000_LinkPaymentsToQuotes")]
public sealed class LinkPaymentsToQuotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "QuoteId",
            table: "Payments",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_QuoteId",
            table: "Payments",
            column: "QuoteId",
            unique: true,
            filter: "[QuoteId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Payments_QuoteId", table: "Payments");
        migrationBuilder.DropColumn(name: "QuoteId", table: "Payments");
    }
}
