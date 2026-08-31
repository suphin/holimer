using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderTransportationPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarrierName",
                table: "PurPurchaseOrder",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "PurPurchaseOrder",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryWarehouseId",
                table: "PurPurchaseOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedFreightAmount",
                table: "PurPurchaseOrder",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedFreightVatRate",
                table: "PurPurchaseOrder",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FreightCurrencyCode",
                table: "PurPurchaseOrder",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<decimal>(
                name: "FreightExchangeRate",
                table: "PurPurchaseOrder",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FreightExchangeRateDate",
                table: "PurPurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FreightExchangeRateSource",
                table: "PurPurchaseOrder",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Sabit");

            migrationBuilder.AddColumn<int>(
                name: "FreightPaymentType",
                table: "PurPurchaseOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedDeliveryDate",
                table: "PurPurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedShipmentDate",
                table: "PurPurchaseOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "PurPurchaseOrder",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransportationNotes",
                table: "PurPurchaseOrder",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransportationType",
                table: "PurPurchaseOrder",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrder_DeliveryWarehouseId",
                table: "PurPurchaseOrder",
                column: "DeliveryWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurPurchaseOrder_PrdWarehouse_DeliveryWarehouseId",
                table: "PurPurchaseOrder",
                column: "DeliveryWarehouseId",
                principalTable: "PrdWarehouse",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurPurchaseOrder_PrdWarehouse_DeliveryWarehouseId",
                table: "PurPurchaseOrder");

            migrationBuilder.DropIndex(
                name: "IX_PurPurchaseOrder_DeliveryWarehouseId",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "CarrierName",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "DeliveryWarehouseId",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "EstimatedFreightAmount",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "EstimatedFreightVatRate",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "FreightCurrencyCode",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "FreightExchangeRate",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "FreightExchangeRateDate",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "FreightExchangeRateSource",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "FreightPaymentType",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "PlannedDeliveryDate",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "PlannedShipmentDate",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "TransportationNotes",
                table: "PurPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "TransportationType",
                table: "PurPurchaseOrder");
        }
    }
}
