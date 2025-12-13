using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVatConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // R-356: SANITIZED - Only constraint removal, no column operations
            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_VatRate",
                table: "Product");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // R-356: SANITIZED - Only constraint restoration, no column operations
            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_VatRate",
                table: "Product",
                sql: "VatRate IN (1,10,20)");
        }
    }
}
