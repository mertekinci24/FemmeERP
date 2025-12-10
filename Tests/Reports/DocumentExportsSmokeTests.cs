using System;
using System.IO;
using System.Threading.Tasks;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Tests.Infrastructure;
using Xunit;

namespace Tests.Reports;

public class DocumentExportsSmokeTests : BaseIntegrationTest
{
    [Fact]
    public async Task ExportFiles_AreCreated_AndNotEmpty()
    {
        // Arrange
        var partner = new Partner { Title = "SmokePartner", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        var doc = new Document { Type = DocumentType.SALES_INVOICE, Number = "S-1", Date = DateTime.Today, Status = DocumentStatus.DRAFT, Partner = partner };
        Ctx.Documents.Add(doc);
        await Ctx.SaveChangesAsync();

        var svc = new DocumentReportService(Ctx);
        var filter = new Application.Documents.DTOs.DocumentListFilter { Page = 1, PageSize = 10, DateFrom = DateTime.Today.AddDays(-1), DateTo = DateTime.Today.AddDays(1), PartnerId = partner.Id };

        var excel = await svc.ExportListExcelAsync(filter);
        var pdf = await svc.ExportListPdfAsync(filter);

        var tmp = Path.Combine(Path.GetTempPath(), "InventoryERP_Exports_Test");
        Directory.CreateDirectory(tmp);
        var baseName = InventoryERP.Presentation.Helpers.ExportFileNameHelper.BuildBaseName(filter.DateFrom, filter.DateTo, partner.Title, filter.SearchText);
        var excelName = InventoryERP.Presentation.Helpers.ExportFileNameHelper.SanitizeFileName(baseName + ".xlsx");
        var pdfName = InventoryERP.Presentation.Helpers.ExportFileNameHelper.SanitizeFileName(baseName + ".pdf");
        var excelPath = Path.Combine(tmp, excelName);
        var pdfPath = Path.Combine(tmp, pdfName);

        File.WriteAllBytes(excelPath, excel);
        File.WriteAllBytes(pdfPath, pdf);

        Assert.True(File.Exists(excelPath));
        Assert.True(new FileInfo(excelPath).Length > 0);
        Assert.True(File.Exists(pdfPath));
        Assert.True(new FileInfo(pdfPath).Length > 0);
    }
}
