using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Persistence;
using Tests.Infrastructure;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Application.Stocks;

namespace Tests.Integration;

public class ExportsSmokeTests : BaseIntegrationTest
{
    [Fact]
    public async Task StockExport_Returns_NonEmpty_Excel()
    {
        // arrange - create product and moves
    var p = new Domain.Entities.Product { Sku = "EXP-1", Name = "ExpProd", BaseUom = "pcs", VatRate = 1 };
        Ctx.Products.Add(p);
        await Ctx.SaveChangesAsync();
        Ctx.StockMoves.Add(new Domain.Entities.StockMove { ItemId = p.Id, Date = DateTime.Now, QtySigned = 1, UnitCost = 5m });
        await Ctx.SaveChangesAsync();

        var svc = new StockExportServiceClosedXml(Ctx);
        var bytes = await svc.ExportMovesExcelAsync(p.Id, null, null);
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public async Task PartnerExport_ExcelAndPdf_Smoke()
    {
        var partner = new Domain.Entities.Partner { Title = "ExpPartner" };
        Ctx.Partners.Add(partner);
        await Ctx.SaveChangesAsync();

    var doc = new Domain.Entities.Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Date = DateTime.Today, Number = "INV-X", Status = Domain.Enums.DocumentStatus.POSTED };
    Ctx.Documents.Add(doc);
    await Ctx.SaveChangesAsync();

    Ctx.PartnerLedgerEntries.Add(new Domain.Entities.PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = DateTime.Today, Debit = 10m, Credit = 0m, AmountTry = 10m, Status = Domain.Enums.LedgerStatus.OPEN });
    await Ctx.SaveChangesAsync();

        var exportSvc = new PartnerExportService(Ctx);
        var xls = await exportSvc.ExportStatementExcelAsync(partner.Id, null, null);
        xls.Should().NotBeNull();
        xls.Length.Should().BeGreaterThan(100);

        var pdf = await exportSvc.ExportStatementPdfAsync(partner.Id, null, null, includeClosed: true);
        pdf.Should().NotBeNull();
        pdf.Length.Should().BeGreaterThan(10);
        // check PDF header
        var header = System.Text.Encoding.ASCII.GetString(pdf, 0, Math.Min(pdf.Length, 4));
        header.Should().StartWith("%PDF");
    }
}
