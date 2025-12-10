using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryERP.Persistence.Migrations
{
    public partial class AddProductDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultWarehouseId",
                table: "Products",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultLocationId",
                table: "Products",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultWarehouseId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DefaultLocationId",
                table: "Products");
        }
    }
}
