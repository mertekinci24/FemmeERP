using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Persistence.Migrations
{
    public partial class AddSequences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentType = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentValue = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_DocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSequences_DocumentType_Year",
                table: "DocumentSequences",
                columns: new[] { "DocumentType", "Year" },
                unique: true);

            // Seed Data
            migrationBuilder.Sql(@"
                INSERT INTO DocumentSequences (DocumentType, Year, CurrentValue, CreatedAt, Version, IsDeleted)
                SELECT
                  Type AS DocumentType,
                  CAST(strftime('%Y', CreatedAt) AS INTEGER) AS Year,
                  COALESCE(MAX(CAST(substr(Number, instr(Number, '-') + 1) AS INTEGER)), 0) AS CurrentValue,
                  strftime('%Y-%m-%d %H:%M:%S', 'now') AS CreatedAt,
                  1 AS Version,
                  0 AS IsDeleted
                FROM Document
                GROUP BY Type, CAST(strftime('%Y', CreatedAt) AS INTEGER);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentSequences");
        }
    }
}
