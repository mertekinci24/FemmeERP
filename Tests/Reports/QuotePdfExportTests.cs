using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Microsoft.Data.Sqlite;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Domain.Enums;

namespace Tests.Reports
{
    /// <summary>
    /// TST-013: R-008 Quote PDF Export Integration Test
    /// </summary>
    public class QuotePdfExportTests
    {
        public QuotePdfExportTests()
        {
            // R-008: Configure QuestPDF license for testing (Community license for non-commercial use)
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        [Fact]
        public async Task GenerateQuotePdfAsync_Returns_Valid_PDF_For_Quote_Document()
        {
            // Arrange: Create in-memory database
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using (var context = new AppDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();

                // Seed: Create a partner (customer)
                var partner = new Partner
                {
                    Name = "Test Customer Inc.",
                    PartnerType = PartnerType.Customer,
                    TaxId = "1234567890",
                    Address = "123 Test Street, Test City",
                    IsActive = true
                };
                context.Partners.Add(partner);
                await context.SaveChangesAsync();

                // Seed: Create a product
                var product = new Product
                {
                    Name = "Test Product",
                    Sku = "TEST-001",
                    BaseUom = "ADET",
                    VatRate = 20,
                    Cost = 100.00m,
                    Active = true
                };
                context.Products.Add(product);
                await context.SaveChangesAsync();

                // Seed: Create a Quote document with lines
                var document = new Document
                {
                    Type = DocumentType.QUOTE,
                    Number = "Q-2025-001",
                    Date = System.DateTime.Now,
                    PartnerId = partner.Id,
                    Status = DocumentStatus.DRAFT,
                    Lines = new System.Collections.Generic.List<DocumentLine>
                    {
                        new DocumentLine
                        {
                            ItemId = product.Id,
                            Qty = 10,
                            UnitPrice = 150.00m,
                            VatRate = 20,
                            Uom = "ADET",
                            Coefficient = 1
                        },
                        new DocumentLine
                        {
                            ItemId = product.Id,
                            Qty = 5,
                            UnitPrice = 150.00m,
                            VatRate = 20,
                            Uom = "ADET",
                            Coefficient = 1
                        }
                    }
                };
                context.Documents.Add(document);
                await context.SaveChangesAsync();

                // Act: Generate PDF using DocumentReportService
                var reportService = new DocumentReportService(context);
                var pdfBytes = await reportService.GenerateQuotePdfAsync(document.Id);

                // Assert: PDF should be generated and non-empty
                pdfBytes.Should().NotBeNull("PDF generation should return byte array");
                pdfBytes.Length.Should().BeGreaterThan(500, "PDF should contain actual content (> 500 bytes)");

                // Additional verification: PDF magic bytes (PDF header starts with %PDF)
                var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4);
                pdfHeader.Should().Be("%PDF", "PDF should have valid PDF header");
            }

            connection.Close();
        }

        [Fact]
        public async Task GenerateQuotePdfAsync_Returns_Empty_For_Non_Existent_Document()
        {
            // Arrange: Create in-memory database with no documents
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using (var context = new AppDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();

                // Act: Try to generate PDF for non-existent document
                var reportService = new DocumentReportService(context);
                var pdfBytes = await reportService.GenerateQuotePdfAsync(999);

                // Assert: Should return empty byte array
                pdfBytes.Should().NotBeNull();
                pdfBytes.Length.Should().Be(0, "Non-existent document should return empty array");
            }

            connection.Close();
        }
    }
}
