using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Services;
using Tests.Infrastructure;
using Xunit;
using Moq;
using InventoryERP.Domain.Interfaces;

namespace Tests.Integration
{
    public class DocumentsDraftCrudTests : BaseIntegrationTest
    {
        [Fact]
        public async Task Create_Update_Delete_Draft_Works()
        {
            var db = Ctx;
            var partner = new Partner { Title = "P-1", Role = PartnerRole.CUSTOMER };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();

            var svc = new DocumentCommandService(db, new InventoryERP.Persistence.Services.InventoryQueriesEf(db));

            var product = new Domain.Entities.Product { Sku = "P-1", Name = "Product 1", BaseUom = "pcs", VatRate = 20 };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var dto = new DocumentDetailDto { Type = "SALES_INVOICE", Number = "D-1", Date = System.DateTime.Today, PartnerId = partner.Id };
            dto.Lines.Add(new DocumentLineDto { ItemId = product.Id, Qty = 2, UnitPrice = 10m, Uom = "pcs", VatRate = 20 });

            var id = await svc.CreateDraftAsync(dto);
            var created = await db.Documents.FindAsync(id);
            Assert.NotNull(created);

            // update
            dto.Number = "D-1-UPDATED";
            dto.Lines.Clear();
            dto.Lines.Add(new DocumentLineDto { ItemId = product.Id, Qty = 1, UnitPrice = 5m, Uom = "pcs", VatRate = 20 });
            await svc.UpdateDraftAsync(id, dto);
            var updated = await db.Documents.FindAsync(id);
            Assert.Equal("D-1-UPDATED", updated!.Number);

            // delete
            await svc.DeleteDraftAsync(id);
            var deleted = await db.Documents.FindAsync(id);
            Assert.Null(deleted);
        }
    }
}
