using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class R131_CashLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CashAccountId",
                table: "Document",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    BankBranch = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Iban = table.Column<string>(type: "TEXT", maxLength: 34, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CashAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocId = table.Column<int>(type: "INTEGER", nullable: true),
                    DocType = table.Column<int>(type: "INTEGER", nullable: true),
                    DocNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    FxRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false, defaultValue: 1.0m),
                    Debit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AmountTry = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashLedgerEntries_CashAccount_CashAccountId",
                        column: x => x.CashAccountId,
                        principalTable: "CashAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashLedgerEntries_Document_DocId",
                        column: x => x.DocId,
                        principalTable: "Document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Document_CashAccountId",
                table: "Document",
                column: "CashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashAccount_Name",
                table: "CashAccount",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashAccount_Type_Currency",
                table: "CashAccount",
                columns: new[] { "Type", "Currency" });

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_CashAccountId_Date",
                table: "CashLedgerEntries",
                columns: new[] { "CashAccountId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_CashAccountId_Status",
                table: "CashLedgerEntries",
                columns: new[] { "CashAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashLedgerEntries_DocId",
                table: "CashLedgerEntries",
                column: "DocId");

            migrationBuilder.AddForeignKey(
                name: "FK_Document_CashAccount_CashAccountId",
                table: "Document",
                column: "CashAccountId",
                principalTable: "CashAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Document_CashAccount_CashAccountId",
                table: "Document");

            migrationBuilder.DropTable(
                name: "CashLedgerEntries");

            migrationBuilder.DropTable(
                name: "CashAccount");

            migrationBuilder.DropIndex(
                name: "IX_Document_CashAccountId",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "CashAccountId",
                table: "Document");
        }
    }
}
