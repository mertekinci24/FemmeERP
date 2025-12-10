using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Tests.Infrastructure;
using Xunit;
using System.Text;

namespace Tests.Reports;

public class DocumentReportsPdfListHeaderTests : BaseIntegrationTest
{
    [Fact]
    public async Task ExportListPdf_Includes_FilterHeader()
    {
        // Arrange: create partner and document
        var partner = new Partner { Title = "FilterPartnerPdf", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        var doc = new Document { Type = DocumentType.SALES_INVOICE, Number = "P-1", Date = DateTime.Today, Status = DocumentStatus.DRAFT, Partner = partner };
        Ctx.Documents.Add(doc);
        await Ctx.SaveChangesAsync();

        var svc = new DocumentReportService(Ctx);
        var filter = new Application.Documents.DTOs.DocumentListFilter { Page = 1, PageSize = 10, DateFrom = DateTime.Today.AddDays(-1), DateTo = DateTime.Today.AddDays(1), PartnerId = partner.Id };

        // Act
        var pdf = await svc.ExportListPdfAsync(filter);

        // Assert
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0, "Generated PDF should not be empty");

        // First try a simple textual search: interpret bytes as Latin1 to preserve bytes-to-chars mapping
        var text = Encoding.Latin1.GetString(pdf);
        if (!text.Contains("Filters:") || !text.Contains("Date:"))
        {
            // Many PDF content streams are compressed (Flate). Try to locate and decompress stream objects and search their contents.
            bool found = false;
            var pdfBytes = pdf;
            var needle = System.Text.Encoding.ASCII.GetBytes("stream");
            var endNeedle = System.Text.Encoding.ASCII.GetBytes("endstream");
            int pos = 0;
            while (pos < pdfBytes.Length)
            {
                // find 'stream'
                int s = IndexOf(pdfBytes, needle, pos);
                if (s < 0) break;
                int e = IndexOf(pdfBytes, endNeedle, s);
                if (e < 0) break;
                var start = s + needle.Length;
                // skip possible CR/LF after 'stream'
                if (start < pdfBytes.Length && (pdfBytes[start] == (byte)'\r' || pdfBytes[start] == (byte)'\n')) start++;
                if (start < pdfBytes.Length && (pdfBytes[start] == (byte)'\n')) start++;
                var length = e - start;
                if (length <= 0) { pos = e + endNeedle.Length; continue; }
                var streamBytes = new byte[length];
                Array.Copy(pdfBytes, start, streamBytes, 0, length);
                // try to decompress with Deflate
                try
                {
                    using var ms2 = new System.IO.MemoryStream(streamBytes);
                    using var ds = new System.IO.Compression.DeflateStream(ms2, System.IO.Compression.CompressionMode.Decompress);
                    using var outMs = new System.IO.MemoryStream();
                    ds.CopyTo(outMs);
                    var decompressed = Encoding.Latin1.GetString(outMs.ToArray());
                    if (decompressed.Contains("Filters:") && decompressed.Contains("Date:")) { found = true; break; }
                }
                catch { /* ignore decompression errors */ }

                pos = e + endNeedle.Length;
            }

            Assert.True(found, "PDF content streams did not contain expected filter header text.");
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int start)
    {
        for (int i = Math.Max(0, start); i <= haystack.Length - needle.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { ok = false; break; }
            }
            if (ok) return i;
        }
        return -1;
    }
    }
