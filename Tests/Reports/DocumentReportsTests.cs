using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Reports;

public class DocumentReportsTests : BaseIntegrationTest
{
    [Fact]
    public async Task BuildInvoicePdf_Returns_NonEmptyBytes()
    {
        // Arrange: create partner, product, document and lines
        var partner = new Partner { Title = "Test Partner", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);

        var product = new Product { Name = "Test Item", Sku = "TST-1", BaseUom = "pcs", VatRate = 20 };
        Ctx.Products.Add(product);
        await Ctx.SaveChangesAsync();

        var doc = new Document
        {
            Type = DocumentType.SALES_INVOICE,
            Number = "INV-001",
            Date = DateTime.Today,
            Status = DocumentStatus.DRAFT,
            PartnerId = partner.Id
        };
        Ctx.Documents.Add(doc);
        await Ctx.SaveChangesAsync();

    var line = new DocumentLine { DocumentId = doc.Id, ItemId = product.Id, Qty = 2, UnitPrice = 10m, VatRate = product.VatRate, Uom = product.BaseUom };
        Ctx.DocumentLines.Add(line);
        await Ctx.SaveChangesAsync();

        var svc = new DocumentReportService(Ctx);

        // Act
        var pdf = await svc.BuildInvoicePdfAsync(doc.Id);

        // Assert
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0, "Generated PDF should not be empty");
    }

    [Fact]
    public async Task ExportListExcel_Returns_NonEmptyBytes()
    {
        // Arrange: ensure at least one document exists
        var partner = new Partner { Title = "X", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        var doc = new Document { Type = DocumentType.SALES_INVOICE, Number = "E-1", Date = DateTime.Today, Status = DocumentStatus.DRAFT, Partner = partner };
        Ctx.Documents.Add(doc);
        await Ctx.SaveChangesAsync();

        var svc = new DocumentReportService(Ctx);
        var filter = new Application.Documents.DTOs.DocumentListFilter { Page = 1, PageSize = 10 };

        // Act
        var excel = await svc.ExportListExcelAsync(filter);

        // Assert
        Assert.NotNull(excel);
        Assert.True(excel.Length > 0, "Generated Excel should not be empty");
    }

    [Fact]
    public async Task ExportListExcel_Includes_FilterHeader()
    {
        // Arrange: create data and supply a filter with date range and partner
        var partner = new Partner { Title = "FilterPartner", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        var doc = new Document { Type = DocumentType.SALES_INVOICE, Number = "F-1", Date = DateTime.Today, Status = DocumentStatus.DRAFT, Partner = partner };
        Ctx.Documents.Add(doc);
        await Ctx.SaveChangesAsync();

    var svc = new DocumentReportService(Ctx);
    var filter = new Application.Documents.DTOs.DocumentListFilter { Page = 1, PageSize = 10, DateFrom = DateTime.Today.AddDays(-1), DateTo = DateTime.Today.AddDays(1), PartnerId = partner.Id };

        // Act
        var excel = await svc.ExportListExcelAsync(filter);

        // Assert: verify top-row contains Filters: and the filter details
        using var ms = new System.IO.MemoryStream(excel);
        using var wb = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = wb.Worksheet("Belgeler");
        var label = ws.Cell(1, 1).GetString();
        var note = ws.Cell(1, 2).GetString();
        Assert.Equal("Filters:", label);
        Assert.Contains("Date:", note);
        Assert.Contains("Partner:", note);
    }
}
