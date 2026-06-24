using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations;

/// <inheritdoc />
public partial class UpdateWarehouse : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_LocationEntity",
            table: "LocationEntity");

        migrationBuilder.DropColumn(
            name: "TotalLocations",
            table: "zones");

        migrationBuilder.DropColumn(
            name: "UsedLocations",
            table: "zones");

        migrationBuilder.DropColumn(
            name: "Description",
            table: "LocationEntity");

        migrationBuilder.DropColumn(
            name: "ZoneCode",
            table: "LocationEntity");

        migrationBuilder.RenameTable(
            name: "LocationEntity",
            newName: "locations");

        migrationBuilder.RenameIndex(
            name: "IX_LocationEntity_ZoneId",
            table: "locations",
            newName: "IX_locations_ZoneId");

        migrationBuilder.AlterColumn<string>(
            name: "UpdatedBy",
            table: "locations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "locations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "locations",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<string>(
            name: "DeletedBy",
            table: "locations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "CreatedBy",
            table: "locations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "AreaId",
            table: "locations",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "BlockId",
            table: "locations",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "WarehouseId",
            table: "locations",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<int>(
            name: "X",
            table: "locations",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Y",
            table: "locations",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Z",
            table: "locations",
            type: "integer",
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_locations",
            table: "locations",
            column: "Id");

        migrationBuilder.CreateTable(
            name: "warehouse_rule_settings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                BlockId = table.Column<Guid>(type: "uuid", nullable: true),
                AreaId = table.Column<Guid>(type: "uuid", nullable: true),
                SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                RuleType = table.Column<int>(type: "integer", nullable: false),
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
                table.PrimaryKey("PK_warehouse_rule_settings", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "warehouses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                table.PrimaryKey("PK_warehouses", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "warehouse_areas",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                table.PrimaryKey("PK_warehouse_areas", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "blocks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                table.PrimaryKey("PK_blocks", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_locations_BlockId",
            table: "locations",
            column: "BlockId");

        migrationBuilder.CreateIndex(
            name: "IX_locations_DeletedAt",
            table: "locations",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_locations_IsDeleted",
            table: "locations",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_locations_TenantId",
            table: "locations",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_locations_WarehouseId",
            table: "locations",
            column: "WarehouseId");

        migrationBuilder.CreateIndex(
            name: "IX_blocks_AreaId_IsDefault",
            table: "blocks",
            columns: new[] { "AreaId", "IsDefault" },
            unique: true,
            filter: "\"IsDefault\" = true");

        migrationBuilder.CreateIndex(
            name: "IX_blocks_DeletedAt",
            table: "blocks",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_blocks_IsDeleted",
            table: "blocks",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_blocks_TenantId",
            table: "blocks",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_areas_DeletedAt",
            table: "warehouse_areas",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_areas_IsDeleted",
            table: "warehouse_areas",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_areas_TenantId",
            table: "warehouse_areas",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_areas_WarehouseId_IsDefault",
            table: "warehouse_areas",
            columns: new[] { "WarehouseId", "IsDefault" },
            unique: true,
            filter: "\"IsDefault\" = true");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_rule_settings_DeletedAt",
            table: "warehouse_rule_settings",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_rule_settings_IsDeleted",
            table: "warehouse_rule_settings",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_rule_settings_TenantId",
            table: "warehouse_rule_settings",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_rule_settings_WarehouseId_LocationId_ZoneId_Block~",
            table: "warehouse_rule_settings",
            columns: new[] { "WarehouseId", "LocationId", "ZoneId", "BlockId", "AreaId", "SkuId", "SupplierId" },
            unique: true)
            .Annotation("Npgsql:NullsDistinct", false);

        migrationBuilder.CreateIndex(
            name: "IX_warehouses_Code",
            table: "warehouses",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_warehouses_DeletedAt",
            table: "warehouses",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_warehouses_IsDeleted",
            table: "warehouses",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_warehouses_TenantId",
            table: "warehouses",
            column: "TenantId");

        migrationBuilder.AddForeignKey(
            name: "FK_inventory_items_locations_LocationId",
            table: "inventory_items",
            column: "LocationId",
            principalTable: "locations",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_locations_zones_ZoneId",
            table: "locations",
            column: "ZoneId",
            principalTable: "zones",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_inventory_items_locations_LocationId",
            table: "inventory_items");

        migrationBuilder.DropForeignKey(
            name: "FK_locations_zones_ZoneId",
            table: "locations");

        migrationBuilder.DropTable(
            name: "blocks");

        migrationBuilder.DropTable(
            name: "warehouse_rule_settings");

        migrationBuilder.DropTable(
            name: "warehouse_areas");

        migrationBuilder.DropTable(
            name: "warehouses");

        migrationBuilder.DropPrimaryKey(
            name: "PK_locations",
            table: "locations");

        migrationBuilder.DropIndex(
            name: "IX_locations_BlockId",
            table: "locations");

        migrationBuilder.DropIndex(
            name: "IX_locations_DeletedAt",
            table: "locations");

        migrationBuilder.DropIndex(
            name: "IX_locations_IsDeleted",
            table: "locations");

        migrationBuilder.DropIndex(
            name: "IX_locations_TenantId",
            table: "locations");

        migrationBuilder.DropIndex(
            name: "IX_locations_WarehouseId",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "AreaId",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "BlockId",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "WarehouseId",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "X",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "Y",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "Z",
            table: "locations");

        migrationBuilder.RenameTable(
            name: "locations",
            newName: "LocationEntity");

        migrationBuilder.RenameIndex(
            name: "IX_locations_ZoneId",
            table: "LocationEntity",
            newName: "IX_LocationEntity_ZoneId");

        migrationBuilder.AddColumn<int>(
            name: "TotalLocations",
            table: "zones",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "UsedLocations",
            table: "zones",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<string>(
            name: "UpdatedBy",
            table: "LocationEntity",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "LocationEntity",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "LocationEntity",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);

        migrationBuilder.AlterColumn<string>(
            name: "DeletedBy",
            table: "LocationEntity",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "CreatedBy",
            table: "LocationEntity",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "LocationEntity",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ZoneCode",
            table: "LocationEntity",
            type: "text",
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_LocationEntity",
            table: "LocationEntity",
            column: "Id");

    }
}
