using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInboundWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropIndex(
                name: "IX_inbound_orders_CreatorId",
                table: "inbound_orders");

            migrationBuilder.DropIndex(
                name: "IX_inbound_orders_SupplierId",
                table: "inbound_orders");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "inbound_orders");

            migrationBuilder.RenameColumn(
                name: "InventoryItemId",
                table: "inbound_items",
                newName: "SkuId");

            migrationBuilder.RenameIndex(
                name: "IX_inbound_items_InventoryItemId",
                table: "inbound_items",
                newName: "IX_inbound_items_SkuId");

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomated",
                table: "warehouse_areas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "goods_receipt_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrnNumber = table.Column<string>(type: "text", nullable: false),
                    InboundOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboundReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    PutawayTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_goods_receipt_notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbound_order_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InboundOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InboundReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    QcInspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PutawayTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptNoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedBy = table.Column<string>(type: "text", nullable: false),
                    Step = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_inbound_order_histories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbound_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "text", nullable: false),
                    InboundOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_inbound_receipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbound_workflow_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowOverReceive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    OverReceiveTolerancePercentage = table.Column<decimal>(type: "numeric", nullable: true),
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
                    table.PrimaryKey("PK_inbound_workflow_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "putaway_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PutawayTaskNumber = table.Column<string>(type: "text", nullable: false),
                    InboundOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboundReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    QcInspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_putaway_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "qc_inspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionNumber = table.Column<string>(type: "text", nullable: false),
                    InboundOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboundReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_qc_inspections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_note_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptNoteId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_goods_receipt_note_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goods_receipt_note_items_goods_receipt_notes_GoodsReceiptNo~",
                        column: x => x.GoodsReceiptNoteId,
                        principalTable: "goods_receipt_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inbound_receipt_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    InboundReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_inbound_receipt_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inbound_receipt_items_inbound_receipts_InboundReceiptId",
                        column: x => x.InboundReceiptId,
                        principalTable: "inbound_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inbound_workflow_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepType = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_inbound_workflow_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inbound_workflow_steps_inbound_workflow_configs_WorkflowCon~",
                        column: x => x.WorkflowConfigId,
                        principalTable: "inbound_workflow_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "putaway_task_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    PutawayQuantity = table.Column<int>(type: "integer", nullable: false),
                    TargetLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActualLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PutawayTaskId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_putaway_task_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_putaway_task_items_putaway_tasks_PutawayTaskId",
                        column: x => x.PutawayTaskId,
                        principalTable: "putaway_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qc_inspection_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    PassedQuantity = table.Column<int>(type: "integer", nullable: false),
                    FailedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    QcInspectionId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_qc_inspection_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_qc_inspection_items_qc_inspections_QcInspectionId",
                        column: x => x.QcInspectionId,
                        principalTable: "qc_inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_note_items_DeletedAt",
                table: "goods_receipt_note_items",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_note_items_GoodsReceiptNoteId",
                table: "goods_receipt_note_items",
                column: "GoodsReceiptNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_note_items_IsDeleted",
                table: "goods_receipt_note_items",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_note_items_SkuId",
                table: "goods_receipt_note_items",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_note_items_TenantId",
                table: "goods_receipt_note_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_DeletedAt",
                table: "goods_receipt_notes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_GrnNumber",
                table: "goods_receipt_notes",
                column: "GrnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_IsDeleted",
                table: "goods_receipt_notes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_TenantId",
                table: "goods_receipt_notes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_order_histories_DeletedAt",
                table: "inbound_order_histories",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_order_histories_InboundOrderId",
                table: "inbound_order_histories",
                column: "InboundOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_order_histories_IsDeleted",
                table: "inbound_order_histories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_order_histories_TenantId",
                table: "inbound_order_histories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_order_histories_UserId",
                table: "inbound_order_histories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipt_items_DeletedAt",
                table: "inbound_receipt_items",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipt_items_InboundReceiptId",
                table: "inbound_receipt_items",
                column: "InboundReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipt_items_IsDeleted",
                table: "inbound_receipt_items",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipt_items_SkuId",
                table: "inbound_receipt_items",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipt_items_TenantId",
                table: "inbound_receipt_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipts_DeletedAt",
                table: "inbound_receipts",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipts_IsDeleted",
                table: "inbound_receipts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipts_ReceiptNumber",
                table: "inbound_receipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbound_receipts_TenantId",
                table: "inbound_receipts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_workflow_configs_DeletedAt",
                table: "inbound_workflow_configs",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_workflow_configs_IsDeleted",
                table: "inbound_workflow_configs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_workflow_configs_TenantId",
                table: "inbound_workflow_configs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_workflow_steps_DeletedAt",
                table: "inbound_workflow_steps",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_workflow_steps_IsDeleted",
                table: "inbound_workflow_steps",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_workflow_steps_TenantId",
                table: "inbound_workflow_steps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_workflow_steps_WorkflowConfigId",
                table: "inbound_workflow_steps",
                column: "WorkflowConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_putaway_task_items_DeletedAt",
                table: "putaway_task_items",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_putaway_task_items_IsDeleted",
                table: "putaway_task_items",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_putaway_task_items_PutawayTaskId",
                table: "putaway_task_items",
                column: "PutawayTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_putaway_task_items_SkuId",
                table: "putaway_task_items",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_putaway_task_items_TenantId",
                table: "putaway_task_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_putaway_tasks_DeletedAt",
                table: "putaway_tasks",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_putaway_tasks_IsDeleted",
                table: "putaway_tasks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_putaway_tasks_PutawayTaskNumber",
                table: "putaway_tasks",
                column: "PutawayTaskNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_putaway_tasks_TenantId",
                table: "putaway_tasks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_items_DeletedAt",
                table: "qc_inspection_items",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_items_IsDeleted",
                table: "qc_inspection_items",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_items_QcInspectionId",
                table: "qc_inspection_items",
                column: "QcInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_items_SkuId",
                table: "qc_inspection_items",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_items_TenantId",
                table: "qc_inspection_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_DeletedAt",
                table: "qc_inspections",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_InspectionNumber",
                table: "qc_inspections",
                column: "InspectionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_IsDeleted",
                table: "qc_inspections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_TenantId",
                table: "qc_inspections",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "goods_receipt_note_items");

            migrationBuilder.DropTable(
                name: "inbound_order_histories");

            migrationBuilder.DropTable(
                name: "inbound_receipt_items");

            migrationBuilder.DropTable(
                name: "inbound_workflow_steps");

            migrationBuilder.DropTable(
                name: "putaway_task_items");

            migrationBuilder.DropTable(
                name: "qc_inspection_items");

            migrationBuilder.DropTable(
                name: "goods_receipt_notes");

            migrationBuilder.DropTable(
                name: "inbound_receipts");

            migrationBuilder.DropTable(
                name: "inbound_workflow_configs");

            migrationBuilder.DropTable(
                name: "putaway_tasks");

            migrationBuilder.DropTable(
                name: "qc_inspections");

            migrationBuilder.DropColumn(
                name: "IsAutomated",
                table: "warehouse_areas");

            migrationBuilder.RenameColumn(
                name: "SkuId",
                table: "inbound_items",
                newName: "InventoryItemId");

            migrationBuilder.RenameIndex(
                name: "IX_inbound_items_SkuId",
                table: "inbound_items",
                newName: "IX_inbound_items_InventoryItemId");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "inbound_orders",
                type: "uuid",
                nullable: true);




        }
    }
}
