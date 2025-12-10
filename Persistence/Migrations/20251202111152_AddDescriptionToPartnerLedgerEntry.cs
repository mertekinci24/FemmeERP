using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToPartnerLedgerEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashAccount_Name",
                table: "CashAccount");

            migrationBuilder.DropIndex(
                name: "IX_CashAccount_Type_Currency",
                table: "CashAccount");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PartnerLedgerEntry",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "CashAccount",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "CashAccount",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_Barcode",
                table: "Product",
                column: "Barcode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_Barcode",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PartnerLedgerEntry");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "CashAccount",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "CashAccount",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_CashAccount_Name",
                table: "CashAccount",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashAccount_Type_Currency",
                table: "CashAccount",
                columns: new[] { "Type", "Currency" });
        }
    }
}
