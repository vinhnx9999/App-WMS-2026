using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddWcsIntegration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsBlocked",
            table: "locations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsBuffer",
            table: "locations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "Type",
            table: "locations",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "Robot",
            table: "inventory_items",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "WcsBlockId",
            table: "blocks",
            type: "text",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "wcs_sub_task_histories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WcsSubTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                FromStatus = table.Column<int>(type: "integer", nullable: false),
                ToStatus = table.Column<int>(type: "integer", nullable: false),
                Robot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                table.PrimaryKey("PK_wcs_sub_task_histories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "wcs_tasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                WmsPutawayTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                WcsBlockId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                WcsTaskNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                TaskType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
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
                table.PrimaryKey("PK_wcs_tasks", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "wcs_sub_tasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WcsTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                SubTaskCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PalletCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                FromLocationCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ToLocationCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                table.PrimaryKey("PK_wcs_sub_tasks", x => x.Id);
                table.ForeignKey(
                    name: "FK_wcs_sub_tasks_wcs_tasks_WcsTaskId",
                    column: x => x.WcsTaskId,
                    principalTable: "wcs_tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_task_histories_DeletedAt",
            table: "wcs_sub_task_histories",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_task_histories_IsDeleted",
            table: "wcs_sub_task_histories",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_task_histories_TenantId",
            table: "wcs_sub_task_histories",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_task_histories_WcsSubTaskId",
            table: "wcs_sub_task_histories",
            column: "WcsSubTaskId");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_tasks_DeletedAt",
            table: "wcs_sub_tasks",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_tasks_IsDeleted",
            table: "wcs_sub_tasks",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_tasks_SubTaskCode",
            table: "wcs_sub_tasks",
            column: "SubTaskCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_tasks_TenantId",
            table: "wcs_sub_tasks",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_sub_tasks_WcsTaskId",
            table: "wcs_sub_tasks",
            column: "WcsTaskId");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_tasks_DeletedAt",
            table: "wcs_tasks",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_tasks_IsDeleted",
            table: "wcs_tasks",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_tasks_TenantId",
            table: "wcs_tasks",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_wcs_tasks_WcsTaskNumber",
            table: "wcs_tasks",
            column: "WcsTaskNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_wcs_tasks_WmsPutawayTaskId",
            table: "wcs_tasks",
            column: "WmsPutawayTaskId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "wcs_sub_task_histories");

        migrationBuilder.DropTable(
            name: "wcs_sub_tasks");

        migrationBuilder.DropTable(
            name: "wcs_tasks");

        migrationBuilder.DropColumn(
            name: "IsBlocked",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "IsBuffer",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "Type",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "Robot",
            table: "inventory_items");

        migrationBuilder.DropColumn(
            name: "WcsBlockId",
            table: "blocks");
    }
}
