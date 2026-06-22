using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations;

/// <inheritdoc />
public partial class updateinbound : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Address",
            table: "suppliers",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Code",
            table: "suppliers",
            type: "text",
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Address",
            table: "suppliers");

        migrationBuilder.DropColumn(
            name: "Code",
            table: "suppliers");
    }
}
