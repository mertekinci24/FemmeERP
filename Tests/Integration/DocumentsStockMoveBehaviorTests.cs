using System.Threading.Tasks;
using Xunit;
using Tests.Infrastructure;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Moq;

namespace Tests.Integration
{
    public class DocumentsStockMoveBehaviorTests : BaseIntegrationTest
    {
        [Fact]
        public async Task Approving_SalesInvoice_DoesNotCreate_StockMoves()
        {
            // Arrange: product + partner
            var p = new Product { Sku = "P1", Name = "P1", BaseUom = "pcs", VatRate = 20 };
            Ctx.Products.Add(p);
            var partner = new Partner { Title = "C1", Role = PartnerRole.CUSTOMER };
            Ctx.Partners.Add(partner);
            await Ctx.SaveChangesAsync();

            // create draft doc with one line (SALES_INVOICE)
            var docDto = new Application.Documents.DTOs.DocumentDetailDto { Type = "SALES_INVOICE", Number = "SI-1", Date = System.DateTime.Today, PartnerId = partner.Id };
            docDto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto { ItemId = p.Id, Qty = 5m, UnitPrice = 10m, Uom = "pcs", VatRate = 20 });

            var svc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var id = await svc.CreateDraftAsync(docDto);

            // Act - approve
            await svc.ApproveAsync(id);

            // Assert - no stock moves created for that doc's lines
            var moves = await Ctx.StockMoves.Where(sm => sm.DocLineId != 0).ToListAsync();
            Assert.Empty(moves);
        }

        [Fact]
        public async Task Approving_SevkIrsaliyesi_Creates_StockMoves()
        {
            // Arrange: product + partner
            var p = new Product { Sku = "P2", Name = "P2", BaseUom = "pcs", VatRate = 20 };
            Ctx.Products.Add(p);
            var partner = new Partner { Title = "C2", Role = PartnerRole.CUSTOMER };
            Ctx.Partners.Add(partner);
            await Ctx.SaveChangesAsync();

            // create draft doc with one line (SEVK_IRSALIYESI)
            var docDto = new Application.Documents.DTOs.DocumentDetailDto { Type = "SEVK_IRSALIYESI", Number = "S-1", Date = System.DateTime.Today, PartnerId = partner.Id };
            docDto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto { ItemId = p.Id, Qty = 3m, UnitPrice = 10m, Uom = "pcs", VatRate = 20 });

            var svc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var id = await svc.CreateDraftAsync(docDto);

            // Act - approve
            await svc.ApproveAsync(id);

            // Assert - stock moves created and qty signed is negative
            var line = await Ctx.DocumentLines.FirstOrDefaultAsync(dl => dl.DocumentId == id);
            Assert.NotNull(line);
            var moves = await Ctx.StockMoves.Where(sm => sm.DocLineId == line.Id).ToListAsync();
            Assert.NotEmpty(moves);
            Assert.All(moves, m => Assert.True(m.QtySigned < 0));
        }
    }
}
