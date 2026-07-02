using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations;

/// <inheritdoc />
public partial class UpdatePutaway : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "MaxQtyInPallet",
            table: "skus",
            type: "integer",
            nullable: false,
            defaultValue: 100);

        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiryDate",
            table: "putaway_task_items",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LotNumber",
            table: "putaway_task_items",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PalletId",
            table: "putaway_task_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SerialNumber",
            table: "putaway_task_items",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "SupplierId",
            table: "putaway_task_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsMixSku",
            table: "pallets",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MaxQtyInPallet",
            table: "skus");

        migrationBuilder.DropColumn(
            name: "ExpiryDate",
            table: "putaway_task_items");

        migrationBuilder.DropColumn(
            name: "LotNumber",
            table: "putaway_task_items");

        migrationBuilder.DropColumn(
            name: "PalletId",
            table: "putaway_task_items");

        migrationBuilder.DropColumn(
            name: "SerialNumber",
            table: "putaway_task_items");

        migrationBuilder.DropColumn(
            name: "SupplierId",
            table: "putaway_task_items");

        migrationBuilder.DropColumn(
            name: "IsMixSku",
            table: "pallets");
    }
}
