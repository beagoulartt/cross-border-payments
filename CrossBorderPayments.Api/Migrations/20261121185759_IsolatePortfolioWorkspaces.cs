using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossBorderPayments.Api.Migrations
{
    /// <inheritdoc />
    public partial class IsolatePortfolioWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SimulatedCompletionAtUtc",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Beneficiaries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Payments_WorkspaceId",
                table: "Payments",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_WorkspaceId",
                table: "Beneficiaries",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_WorkspaceId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Beneficiaries_WorkspaceId",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "SimulatedCompletionAtUtc",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Beneficiaries");
        }
    }
}
