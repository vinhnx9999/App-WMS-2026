using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWcsSubTaskCode_AddCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wcs_sub_tasks_SubTaskCode",
                table: "wcs_sub_tasks");

            migrationBuilder.DropIndex(
                name: "IX_wcs_sub_tasks_WcsTaskId",
                table: "wcs_sub_tasks");

            migrationBuilder.DropColumn(
                name: "SubTaskCode",
                table: "wcs_sub_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_wcs_sub_tasks_WcsTaskId_PalletCode",
                table: "wcs_sub_tasks",
                columns: new[] { "WcsTaskId", "PalletCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wcs_sub_tasks_WcsTaskId_PalletCode",
                table: "wcs_sub_tasks");

            migrationBuilder.AddColumn<string>(
                name: "SubTaskCode",
                table: "wcs_sub_tasks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_wcs_sub_tasks_SubTaskCode",
                table: "wcs_sub_tasks",
                column: "SubTaskCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wcs_sub_tasks_WcsTaskId",
                table: "wcs_sub_tasks",
                column: "WcsTaskId");
        }
    }
}
