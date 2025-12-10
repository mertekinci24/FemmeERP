using System;
using System.IO;
using System.Threading.Tasks;
using InventoryERP.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace InventoryERP.Infrastructure.Services;

public sealed class ReportingService : IReportingService
{
    public Task<byte[]> GenerateAsync(string title, string content)
    {
        // QuestPDF Document with simple text content (original R-008 intent)
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Content().Column(col =>
                {
                    col.Item().Text(title ?? "Report").SemiBold().FontSize(18);
                    col.Item().PaddingTop(12).Text(content ?? string.Empty).FontSize(12);
                });
            });
        });

        using var ms = new MemoryStream();
        // License for tests
        QuestPDF.Settings.License = LicenseType.Community;
        doc.GeneratePdf(ms);
        return Task.FromResult(ms.ToArray());
    }
}
