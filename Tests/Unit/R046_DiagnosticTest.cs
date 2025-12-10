using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using FluentAssertions;
using Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Tests.Infrastructure;
using InventoryERP.Infrastructure.Services;

namespace Tests.Unit
{
    public class R046_DiagnosticTest : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _dbContext;
        private readonly IServiceProvider _serviceProvider;

        public R046_DiagnosticTest()
        {
            var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
            _serviceProvider = provider;
            _connection = conn;
            _dbContext = provider.GetRequiredService<AppDbContext>();
        }

        [Fact]
        public async Task SaveAdjustmentSlip_WithQtyEntered_ShouldSucceed()
        {
            // Arrange: Setup in-memory database with test data
            var testProduct = new InventoryERP.Domain.Entities.Product
            {
                Sku = "TEST-R046",
                Name = "Product for R-046 Diagnostic",
                BaseUom = "EA",
                VatRate = 20
            };
            _dbContext.Products.Add(testProduct);
            await _dbContext.SaveChangesAsync();

            var docSvc = _serviceProvider.GetRequiredService<IDocumentCommandService>();

            var draftDto = new DocumentDetailDto
            {
                Type = "ADJUSTMENT_OUT", // STOK_CIKIS_FISI
                Date = DateTime.Today,
                Lines = new System.Collections.Generic.List<DocumentLineDto>
                {
                    new DocumentLineDto
                    {
                        ItemId = testProduct.Id,
                        Qty = 1,
                        Uom = testProduct.BaseUom, // R-046: Add Uom (REQUIRED field)
                        VatRate = testProduct.VatRate
                    }
                }
            };

            // Act 1: Create Draft
            int draftId = await docSvc.CreateDraftAsync(draftDto);

            // Assert 1
            draftId.Should().BeGreaterThan(0);

            // Act 2: Update Draft (simulating "Kaydet")
            draftDto.Lines[0].Qty = 5;
            await docSvc.UpdateDraftAsync(draftId, draftDto);

            // Assert 2
            var doc = await _dbContext.Documents.FindAsync(draftId);
            doc.Should().NotBeNull();
            // Verify lines
            var lines = await _dbContext.DocumentLines.Where(l => l.DocumentId == draftId).ToListAsync();
            lines.Should().HaveCount(1);
            lines[0].Qty.Should().Be(5);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
