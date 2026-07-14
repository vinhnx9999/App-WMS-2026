using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingAttributesToReceiptItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "inbound_orders");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiryDate",
                table: "inbound_receipt_items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "inbound_receipt_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "inbound_receipt_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "inbound_receipt_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiryDate",
                table: "inbound_items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "inbound_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "inbound_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "inbound_items",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "inbound_receipt_items");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "inbound_receipt_items");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "inbound_receipt_items");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "inbound_receipt_items");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "inbound_items");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "inbound_items");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "inbound_items");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "inbound_items");

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "inbound_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
