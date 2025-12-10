using System.Threading.Tasks;
using Xunit;
using Tests.Infrastructure;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Moq;
using InventoryERP.Domain.Interfaces;

namespace Tests.Integration
{
    public class DocumentsLotExpiryTests : BaseIntegrationTest
    {
        [Fact]
        public async Task Incoming_Goods_Creates_Lot_And_Associates_StockMove()
        {
            // Arrange
            var partner = new Partner { Title = "Supplier1", Role = Domain.Enums.PartnerRole.SUPPLIER };
            Ctx.Partners.Add(partner);
            var product = new Product { Sku = "LOT1", Name = "LotProduct", BaseUom = "ADET", VatRate = 10, ReservedQty = 0m };
            Ctx.Products.Add(product);
            await Ctx.SaveChangesAsync();

            var docSvc = new DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
            var dto = new Application.Documents.DTOs.DocumentDetailDto { Type = "GELEN_IRSALIYE", Number = "G-1", Date = System.DateTime.Today, PartnerId = partner.Id };
            var expiry = new System.DateTime(2026, 1, 1);
            dto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto { ItemId = product.Id, Qty = 10m, UnitPrice = 0m, Uom = "ADET", VatRate = 10, LotNumber = "L-001", ExpiryDate = expiry });

            // Act - create draft which should create Lot
            var id = await docSvc.CreateDraftAsync(dto);

            // Assert Lot created and DocumentLine linked
            var lot = await Ctx.Lots.FirstOrDefaultAsync(l => l.LotNumber == "L-001" && l.ProductId == product.Id);
            lot.Should().NotBeNull();
            lot!.ExpiryDate.Should().Be(expiry);

            var doc = await Ctx.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id);
            doc.Should().NotBeNull();
            var line = doc!.Lines.FirstOrDefault();
            line.Should().NotBeNull();
            line!.LotId.Should().Be(lot.Id);

            // Act - approve to create StockMove
            await docSvc.ApproveAsync(id);

            // Assert StockMove exists for the doc line and traceable to Lot via DocLine
            var sm = await Ctx.StockMoves.Include(s => s.DocumentLine).FirstOrDefaultAsync(s => s.DocLineId == line.Id);
            sm.Should().NotBeNull();
            // DocumentLine has LotId set
            sm!.DocumentLine.Should().NotBeNull();
            sm.DocumentLine!.LotId.Should().Be(lot.Id);
        }
    }
}
