using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Partners;
using ClosedXML.Excel;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryERP.Infrastructure.Services
{
    public class PartnerExportService : IPartnerExportService
    {
        private readonly AppDbContext _db;
        public PartnerExportService(AppDbContext db) => _db = db;

        public async Task<byte[]> ExportStatementExcelAsync(int partnerId, DateOnly? from, DateOnly? to)
        {
            var query = _db.PartnerLedgerEntries.AsNoTracking().Where(p => p.PartnerId == partnerId);
            if (from is not null) query = query.Where(p => p.Date >= from.Value.ToDateTime(new TimeOnly(0, 0)));
            if (to is not null) query = query.Where(p => p.Date <= to.Value.ToDateTime(new TimeOnly(23, 59, 59)));

            var rows = await query.OrderBy(p => p.Date).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ekstre");
            ws.Cell(1, 1).Value = "Tarih";
            ws.Cell(1, 2).Value = "Belge Türü";
            ws.Cell(1, 3).Value = "Belge No";
            ws.Cell(1, 4).Value = "Borç";
            ws.Cell(1, 5).Value = "Alacak";
            ws.Cell(1, 6).Value = "Tutar (TRY)";
            ws.Cell(1, 7).Value = "Bakiye";

            var rowIndex = 2;
            decimal running = 0;
            foreach (var entry in rows)
            {
                running += entry.Debit - entry.Credit;
                ws.Cell(rowIndex, 1).Value = entry.Date;
                ws.Cell(rowIndex, 2).Value = entry.DocType?.ToString() ?? string.Empty;
                ws.Cell(rowIndex, 3).Value = entry.DocNumber;
                ws.Cell(rowIndex, 4).Value = entry.Debit;
                ws.Cell(rowIndex, 5).Value = entry.Credit;
                ws.Cell(rowIndex, 6).Value = entry.AmountTry;
                ws.Cell(rowIndex, 7).Value = running;
                rowIndex++;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<byte[]> ExportStatementPdfAsync(int partnerId, DateOnly? from, DateOnly? to, bool includeClosed)
        {
            var query = _db.PartnerLedgerEntries
                .AsNoTracking()
                .Where(p => p.PartnerId == partnerId);

            if (!includeClosed)
            {
                query = query.Where(p => p.Status == LedgerStatus.OPEN);
            }
            else
            {
                query = query.Where(p => p.Status != LedgerStatus.CANCELED);
            }

            if (from is not null) query = query.Where(p => p.Date >= from.Value.ToDateTime(new TimeOnly(0, 0)));
            if (to is not null) query = query.Where(p => p.Date <= to.Value.ToDateTime(new TimeOnly(23, 59, 59)));

            var rows = await query.OrderBy(p => p.Date).ToListAsync();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Ekstre ({(from?.ToString("yyyy-MM-dd") ?? "Başlangıç")} - {(to?.ToString("yyyy-MM-dd") ?? "Bitiş")})").Bold();
                        col.Item().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Tarih").Bold();
                                header.Cell().Text("Belge").Bold();
                                header.Cell().Text("Borç").Bold();
                                header.Cell().Text("Alacak").Bold();
                                header.Cell().Text("Bakiye").Bold();
                            });

                            decimal running = 0m;
                            foreach (var entry in rows)
                            {
                                running += entry.Debit - entry.Credit;
                                table.Cell().Text(entry.Date.ToString("yyyy-MM-dd"));
                                table.Cell().Text(entry.DocNumber ?? string.Empty);
                                table.Cell().Text(entry.Debit.ToString("N2"));
                                table.Cell().Text(entry.Credit.ToString("N2"));
                                table.Cell().Text(running.ToString("N2"));
                            }
                        });
                    });
                });
            });

            QuestPDF.Settings.License = LicenseType.Community;
            using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            return ms.ToArray();
        }

        public async Task<byte[]> ExportAgingExcelAsync(int partnerId, DateOnly asOf)
        {
            var asOfDt = asOf.ToDateTime(new TimeOnly(23, 59, 59));
            var entries = await _db.PartnerLedgerEntries.AsNoTracking()
                .Where(p => p.PartnerId == partnerId && p.Status == LedgerStatus.OPEN)
                .ToListAsync();

            var (b0, b30, b60, b90) = CalculateBuckets(entries, asOfDt);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Yaşlandırma");
            ws.Cell(1, 1).Value = "0-30";
            ws.Cell(1, 2).Value = "31-60";
            ws.Cell(1, 3).Value = "61-90";
            ws.Cell(1, 4).Value = "90+";
            ws.Cell(2, 1).Value = b0;
            ws.Cell(2, 2).Value = b30;
            ws.Cell(2, 3).Value = b60;
            ws.Cell(2, 4).Value = b90;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<byte[]> ExportAgingPdfAsync(int partnerId, DateOnly asOf)
        {
            var asOfDt = asOf.ToDateTime(new TimeOnly(23, 59, 59));
            var entries = await _db.PartnerLedgerEntries.AsNoTracking()
                .Where(p => p.PartnerId == partnerId && p.Status == LedgerStatus.OPEN)
                .ToListAsync();

            var (b0, b30, b60, b90) = CalculateBuckets(entries, asOfDt);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Yaşlandırma (AsOf: {asOf:yyyy-MM-dd})").Bold();
                        col.Item().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                header.Cell().Text("Dilime").Bold();
                                header.Cell().Text("Tutar (TRY)").Bold();
                            });

                            table.Cell().Text("0-30");
                            table.Cell().Text(b0.ToString("N2"));
                            table.Cell().Text("31-60");
                            table.Cell().Text(b30.ToString("N2"));
                            table.Cell().Text("61-90");
                            table.Cell().Text(b60.ToString("N2"));
                            table.Cell().Text("90+");
                            table.Cell().Text(b90.ToString("N2"));
                        });
                    });
                });
            });

            QuestPDF.Settings.License = LicenseType.Community;
            using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            return ms.ToArray();
        }

        private static (decimal b0, decimal b30, decimal b60, decimal b90) CalculateBuckets(System.Collections.Generic.IEnumerable<InventoryERP.Domain.Entities.PartnerLedgerEntry> entries, DateTime asOfDt)
        {
            decimal b0 = 0m, b30 = 0m, b60 = 0m, b90 = 0m;
            foreach (var entry in entries)
            {
                if (entry.DueDate is null) continue;
                var age = (asOfDt - entry.DueDate.Value).TotalDays;
                var amount = entry.Debit - entry.Credit;
                if (age <= 30) b0 += amount;
                else if (age <= 60) b30 += amount;
                else if (age <= 90) b60 += amount;
                else b90 += amount;
            }

            return (b0, b30, b60, b90);
        }
    }
}
