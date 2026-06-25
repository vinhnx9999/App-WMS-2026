using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations;

/// <inheritdoc />
public partial class UpdateInventory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // migrationBuilder.DropForeignKey(
        //     name: "FK_inbound_items_inventory_items_InventoryItemId",
        //     table: "inbound_items");

        // migrationBuilder.DropForeignKey(
        //     name: "FK_inventory_items_locations_LocationId",
        //     table: "inventory_items");

        // migrationBuilder.DropForeignKey(
        //     name: "FK_inventory_items_skus_SkuId",
        //     table: "inventory_items");

        // migrationBuilder.DropForeignKey(
        //     name: "FK_outbound_items_inventory_items_InventoryItemId",
        //     table: "outbound_items");

        migrationBuilder.DropIndex(
            name: "IX_inventory_items_SkuId",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "Barcode",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "CategoryName",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "Description",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "LocationName",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "Name",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "SkuCode",
            table: "inventory_items");

        migrationBuilder.RenameColumn(
            name: "Z",
            table: "locations",
            newName: "CoorZ");

        migrationBuilder.RenameColumn(
            name: "Y",
            table: "locations",
            newName: "CoorY");

        migrationBuilder.RenameColumn(
            name: "X",
            table: "locations",
            newName: "CoorX");

        migrationBuilder.RenameColumn(
            name: "ZoneName",
            table: "inventory_items",
            newName: "SerialNumber");

        migrationBuilder.RenameColumn(
            name: "MinQuantity",
            table: "inventory_items",
            newName: "AllocatedQuantity");

        migrationBuilder.RenameColumn(
            name: "CategoryId",
            table: "inventory_items",
            newName: "SupplierId");

        migrationBuilder.AddColumn<string>(
            name: "Barcode",
            table: "skus",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MinQuantity",
            table: "skus",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<Guid>(
            name: "SkuId",
            table: "inventory_items",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "LocationId",
            table: "inventory_items",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiryDate",
            table: "inventory_items",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PalletId",
            table: "inventory_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PutawayDate",
            table: "inventory_items",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "inventory_items",
            type: "bytea",
            nullable: false,
            defaultValue: Array.Empty<byte>());

        migrationBuilder.CreateTable(
            name: "pallets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PalletCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Material = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                Weight = table.Column<decimal>(type: "numeric", nullable: true),
                Length = table.Column<decimal>(type: "numeric", nullable: true),
                Width = table.Column<decimal>(type: "numeric", nullable: true),
                Height = table.Column<decimal>(type: "numeric", nullable: true),
                MaxLoadCapacity = table.Column<decimal>(type: "numeric", nullable: true),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_pallets", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_TenantId_SkuId_LocationId_SupplierId_Serial~",
            table: "inventory_items",
            columns: new[] { "TenantId", "SkuId", "LocationId", "SupplierId", "SerialNumber", "PalletId", "ExpiryDate" },
            unique: true,
            filter: "\"IsDeleted\" = false")
            .Annotation("Npgsql:NullsDistinct", false);

        migrationBuilder.CreateIndex(
            name: "IX_pallets_DeletedAt",
            table: "pallets",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_pallets_IsDeleted",
            table: "pallets",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_pallets_TenantId",
            table: "pallets",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_pallets_TenantId_PalletCode",
            table: "pallets",
            columns: new[] { "TenantId", "PalletCode" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "pallets");

        migrationBuilder.DropIndex(
            name: "IX_inventory_items_TenantId_SkuId_LocationId_SupplierId_Serial~",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "Barcode",
            table: "skus");

        migrationBuilder.DropColumn(
            name: "MinQuantity",
            table: "skus");

        migrationBuilder.DropColumn(
            name: "ExpiryDate",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "PalletId",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "PutawayDate",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "inventory_items");

        migrationBuilder.RenameColumn(
            name: "CoorZ",
            table: "locations",
            newName: "Z");

        migrationBuilder.RenameColumn(
            name: "CoorY",
            table: "locations",
            newName: "Y");

        migrationBuilder.RenameColumn(
            name: "CoorX",
            table: "locations",
            newName: "X");

        migrationBuilder.RenameColumn(
            name: "SupplierId",
            table: "inventory_items",
            newName: "CategoryId");

        migrationBuilder.RenameColumn(
            name: "SerialNumber",
            table: "inventory_items",
            newName: "ZoneName");

        migrationBuilder.RenameColumn(
            name: "AllocatedQuantity",
            table: "inventory_items",
            newName: "MinQuantity");

        migrationBuilder.AlterColumn<Guid>(
            name: "SkuId",
            table: "inventory_items",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "LocationId",
            table: "inventory_items",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<string>(
            name: "Barcode",
            table: "inventory_items",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CategoryName",
            table: "inventory_items",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "inventory_items",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LocationName",
            table: "inventory_items",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Name",
            table: "inventory_items",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "SkuCode",
            table: "inventory_items",
            type: "text",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_SkuId",
            table: "inventory_items",
            column: "SkuId",
            unique: true);

        // migrationBuilder.AddForeignKey(
        //     name: "FK_inbound_items_inventory_items_InventoryItemId",
        //     table: "inbound_items",
        //     column: "InventoryItemId",
        //     principalTable: "inventory_items",
        //     principalColumn: "Id",
        //     onDelete: ReferentialAction.Cascade);

        // migrationBuilder.AddForeignKey(
        //     name: "FK_inventory_items_locations_LocationId",
        //     table: "inventory_items",
        //     column: "LocationId",
        //     principalTable: "locations",
        //     principalColumn: "Id");

        // migrationBuilder.AddForeignKey(
        //     name: "FK_inventory_items_skus_SkuId",
        //     table: "inventory_items",
        //     column: "SkuId",
        //     principalTable: "skus",
        //     principalColumn: "Id");

        // migrationBuilder.AddForeignKey(
        //     name: "FK_outbound_items_inventory_items_InventoryItemId",
        //     table: "outbound_items",
        //     column: "InventoryItemId",
        //     principalTable: "inventory_items",
        //     principalColumn: "Id",
        //     onDelete: ReferentialAction.Cascade);
    }
}
