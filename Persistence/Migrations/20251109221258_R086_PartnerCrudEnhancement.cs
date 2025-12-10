using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class R086_PartnerCrudEnhancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Partner",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "TaxNo",
                table: "Partner",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Partner",
                newName: "PartnerType");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Partner",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Partner",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Partner",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "Partner",
                type: "TEXT",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Partner",
                type: "TEXT",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "Partner");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Partner");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Partner",
                newName: "TaxNo");

            migrationBuilder.RenameColumn(
                name: "PartnerType",
                table: "Partner",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Partner",
                newName: "Title");
        }
    }
}
