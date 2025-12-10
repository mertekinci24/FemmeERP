using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InventoryERP.Infrastructure.Reporting;

namespace InventoryERP.Infrastructure.Services
{
    public interface IPrintService
    {
        Task<byte[]> GeneratePdfAsync(DocumentDetailDto doc);
        Task<byte[]> GenerateProductLabelAsync(ProductRowDto product);
        Task<byte[]> GenerateInvoicePdfAsync(DocumentDetailDto doc);
    }

    public class PrintService : IPrintService
    {
        private readonly ICompanyService _companyService;

        public PrintService(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        public Task<byte[]> GeneratePdfAsync(DocumentDetailDto doc)
        {
            QuestPdfConfig.EnsureInitialized();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);
                    page.Header().Text(text =>
                    {
                        text.Span("Belge: ").SemiBold();
                        text.Span(doc.Number ?? "(Numarasız)");
                    });
                    page.Content().Text(text =>
                    {
                        text.Span("Tarih: ").SemiBold();
                        text.Span(doc.Date.ToString("dd.MM.yyyy"));
                    });
                    page.Footer().AlignRight().Text(x => x.Span("InventoryERP"));
                });
            });

            var bytes = document.GeneratePdf();
            return Task.FromResult(bytes);
        }

        public Task<byte[]> GenerateProductLabelAsync(ProductRowDto product)
        {
            QuestPdfConfig.EnsureInitialized();
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A6);
                    page.Content().Stack(stack =>
                    {
                        stack.Item().Text(product.Name).Bold().FontSize(14);
                        stack.Item().Text($"SKU: {product.Sku}");
                        stack.Item().Text($"Birim: {product.BaseUom}");
                        stack.Item().Text($"KDV: {product.VatRate}%");
                    });
                });
            });

            var bytes = document.GeneratePdf();
            return Task.FromResult(bytes);
        }

        public Task<byte[]> GenerateInvoicePdfAsync(DocumentDetailDto doc)
        {
            QuestPdfConfig.EnsureInitialized();
            var company = _companyService.GetCompanyProfile();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Stack(stack =>
                            {
                                stack.Item().Text("LOGO").Bold().FontSize(16);
                                stack.Item().Text(company.Name).Bold().FontSize(14);
                                stack.Item().Text(company.Address).FontSize(10);
                                stack.Item().Text($"VKN: {company.Vkn}   Mersis: {company.Mersis}").FontSize(10);
                            });
                            row.RelativeItem().AlignRight().Stack(stack =>
                            {
                                stack.Item().Text("FATURA").Bold().FontSize(18);
                                stack.Item().Text($"No: {doc.Number}").FontSize(11);
                                stack.Item().Text($"Tarih: {doc.Date:dd.MM.yyyy}").FontSize(11);
                                stack.Item().Text($"Saat: {doc.Date:HH:mm}").FontSize(11);
                            });
                        });

                        col.Item().PaddingVertical(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                        col.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(box =>
                        {
                            box.Item().Text($"Sayın {doc.PartnerTitle}").Bold();
                            box.Item().Text($"Adres: {(string.IsNullOrWhiteSpace(doc.PartnerAddress) ? "-" : doc.PartnerAddress)}");
                            box.Item().Text($"VKN/TCKN: {(string.IsNullOrWhiteSpace(doc.PartnerTaxId) ? "-" : doc.PartnerTaxId)}    Vergi Dairesi: {(string.IsNullOrWhiteSpace(doc.PartnerTaxOffice) ? "-" : doc.PartnerTaxOffice)}");
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.6f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.4f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Sıra No").SemiBold();
                                header.Cell().Element(HeaderCell).Text("Stok Kodu").SemiBold();
                                header.Cell().Element(HeaderCell).Text("Ürün Adı").SemiBold();
                                header.Cell().Element(HeaderCell).Text("Miktar").SemiBold();
                                header.Cell().Element(HeaderCell).Text("Birim").SemiBold();
                                header.Cell().Element(HeaderCell).Text("Birim Fiyat").SemiBold();
                                header.Cell().Element(HeaderCell).Text("İskonto").SemiBold();
                                header.Cell().Element(HeaderCell).Text("KDV %").SemiBold();
                                header.Cell().Element(HeaderCell).Text("Tutar").SemiBold();
                            });

                            if (doc.Lines != null && doc.Lines.Any())
                            {
                                var index = 1;
                                foreach (var line in doc.Lines)
                                {
                                    var discount = 0m;
                                    var net = line.Qty * line.UnitPrice * (1 - discount);
                                    var vat = net * (line.VatRate / 100m);
                                    var gross = net + vat;
                                    var shaded = index % 2 == 0;
                                    table.Cell().Element(c => BodyCell(c, shaded)).AlignRight().Text(index.ToString());
                                    table.Cell().Element(c => BodyCell(c, shaded)).Text(line.ItemId.ToString());
                                    table.Cell().Element(c => BodyCell(c, shaded)).Text(line.ItemName ?? string.Empty);
                                    table.Cell().Element(c => BodyCell(c, shaded)).AlignRight().Text(line.Qty.ToString("N2"));
                                    table.Cell().Element(c => BodyCell(c, shaded)).Text(line.Uom ?? string.Empty);
                                    table.Cell().Element(c => BodyCell(c, shaded)).AlignRight().Text(line.UnitPrice.ToString("N2"));
                                    table.Cell().Element(c => BodyCell(c, shaded)).AlignRight().Text(discount == 0 ? "-" : discount.ToString("P0"));
                                    table.Cell().Element(c => BodyCell(c, shaded)).AlignRight().Text(line.VatRate.ToString("N0"));
                                    table.Cell().Element(c => BodyCell(c, shaded)).AlignRight().Text(gross.ToString("N2"));
                                    index++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(9).Padding(8).Text("Satır bulunamadı.");
                            }
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text($"Yalnız: {NumberToText(doc.TotalGross)}").Italic();
                                left.Item().PaddingTop(4).Text("Notlar: ");
                                left.Item().PaddingTop(6).Text("IBAN: TR00 0000 0000 0000 0000 0000 00");
                                left.Item().Text("IBAN: TR00 0000 0000 0000 0000 0000 01");
                            });

                            row.RelativeItem(0.9f).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(1);
                                });

                                SummaryRow(table, "Ara Toplam", doc.TotalNet, false);
                                SummaryRow(table, "Toplam KDV", doc.TotalVat, false);
                                SummaryRow(table, "Vergiler Dahil Toplam", doc.TotalGross, true);
                            });
                        });
                    });
                });
            });

            return Task.FromResult(document.GeneratePdf());

            static IContainer HeaderCell(IContainer container) =>
                container.Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Medium).Padding(4);

            static IContainer BodyCell(IContainer container, bool shaded) =>
                shaded
                    ? container.Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    : container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

            static void SummaryRow(TableDescriptor table, string label, decimal value, bool bold)
            {
                table.Cell().Element(e => e.Padding(4)).Text(label).SemiBold();
                var cell = table.Cell().Element(e => e.Padding(4).AlignRight());
                if (bold)
                    cell.Text(value.ToString("N2")).SemiBold();
                else
                    cell.Text(value.ToString("N2"));
            }

            static string NumberToText(decimal value) => $"({value:N2}) TL";
        }
    }
}
