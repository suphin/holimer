using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasingExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PurSupplierQuotation",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeRateDate",
                table: "PurSupplierQuotation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeRateSource",
                table: "PurSupplierQuotation",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Sabit");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PurPurchaseOrder",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeRateDate",
                table: "PurPurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeRateSource",
                table: "PurPurchaseOrder",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Sabit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PurSupplierQuotation");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "PurSupplierQuotation");

            migrationBuilder.DropColumn(
                name: "ExchangeRateSource",
                table: "PurSupplierQuotation");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "ExchangeRateSource",
                table: "PurPurchaseOrder");
        }
    }
}
