using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatebase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "zones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "webhook_events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "skus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "refresh_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "outbox_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "outbound_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "outbound_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "LocationEntity",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "inventory_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "inbound_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "inbound_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "erp_sync_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "audit_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_zones_IsDeleted",
                table: "zones",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_IsDeleted",
                table: "webhook_events",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_users_IsDeleted",
                table: "users",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_IsDeleted",
                table: "tenants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_IsDeleted",
                table: "suppliers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_skus_IsDeleted",
                table: "skus",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_roles_IsDeleted",
                table: "roles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_IsDeleted",
                table: "refresh_tokens",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_IsDeleted",
                table: "outbox_messages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_orders_IsDeleted",
                table: "outbound_orders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_items_IsDeleted",
                table: "outbound_items",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_IsDeleted",
                table: "inventory_items",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_orders_IsDeleted",
                table: "inbound_orders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_items_IsDeleted",
                table: "inbound_items",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_erp_sync_logs_IsDeleted",
                table: "erp_sync_logs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_customers_IsDeleted",
                table: "customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_categories_IsDeleted",
                table: "categories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_IsDeleted",
                table: "audit_logs",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_zones_IsDeleted",
                table: "zones");

            migrationBuilder.DropIndex(
                name: "IX_webhook_events_IsDeleted",
                table: "webhook_events");

            migrationBuilder.DropIndex(
                name: "IX_users_IsDeleted",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_tenants_IsDeleted",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_suppliers_IsDeleted",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_skus_IsDeleted",
                table: "skus");

            migrationBuilder.DropIndex(
                name: "IX_roles_IsDeleted",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_IsDeleted",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_IsDeleted",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbound_orders_IsDeleted",
                table: "outbound_orders");

            migrationBuilder.DropIndex(
                name: "IX_outbound_items_IsDeleted",
                table: "outbound_items");

            migrationBuilder.DropIndex(
                name: "IX_inventory_items_IsDeleted",
                table: "inventory_items");

            migrationBuilder.DropIndex(
                name: "IX_inbound_orders_IsDeleted",
                table: "inbound_orders");

            migrationBuilder.DropIndex(
                name: "IX_inbound_items_IsDeleted",
                table: "inbound_items");

            migrationBuilder.DropIndex(
                name: "IX_erp_sync_logs_IsDeleted",
                table: "erp_sync_logs");

            migrationBuilder.DropIndex(
                name: "IX_customers_IsDeleted",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_categories_IsDeleted",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_IsDeleted",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "zones");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "outbound_orders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "outbound_items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "LocationEntity");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "inbound_orders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "inbound_items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "erp_sync_logs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "audit_logs");
        }
    }
}
