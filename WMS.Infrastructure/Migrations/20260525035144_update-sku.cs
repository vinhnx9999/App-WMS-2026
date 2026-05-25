using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatesku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_skus_TenantId_SkuCode",
                table: "skus");

            migrationBuilder.CreateIndex(
                name: "IX_skus_TenantId_SkuCode",
                table: "skus",
                columns: new[] { "TenantId", "SkuCode" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_skus_TenantId_SkuCode",
                table: "skus");

            migrationBuilder.CreateIndex(
                name: "IX_skus_TenantId_SkuCode",
                table: "skus",
                columns: new[] { "TenantId", "SkuCode" },
                unique: true);
        }
    }
}
