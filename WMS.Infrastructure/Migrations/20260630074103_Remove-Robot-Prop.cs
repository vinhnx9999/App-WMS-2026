using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRobotProp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inbound_order_histories_UserId",
                table: "inbound_order_histories");

            migrationBuilder.DropColumn(
                name: "Robot",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "inbound_order_histories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Robot",
                table: "inventory_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "inbound_order_histories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_inbound_order_histories_UserId",
                table: "inbound_order_histories",
                column: "UserId");
        }
    }
}
