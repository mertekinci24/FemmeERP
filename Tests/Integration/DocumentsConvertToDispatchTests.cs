using System.Threading.Tasks;
using Xunit;
using Tests.Infrastructure;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using System.Collections.Generic;
using InventoryERP.Application.Documents.DTOs;
using System.Linq;
using Moq;
using InventoryERP.Domain.Interfaces;

namespace Tests.Integration;

public class DocumentsConvertToDispatchTests : BaseIntegrationTest
{
    [Fact]
    public async Task ConvertApprovedSalesOrder_CreatesDraftDispatchWithLines()
    {
        // Arrange: create partner
        var partner = new Partner { Title = "CUST1", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        await Ctx.SaveChangesAsync();

        var svc = new DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));

        // create a product to reference in lines
        var prod = new Domain.Entities.Product { Sku = "P1", Name = "Prod1", BaseUom = "ADET", VatRate = 10, ReservedQty = 0m };
        Ctx.Products.Add(prod);
        await Ctx.SaveChangesAsync();

        // create sales order draft with two lines
        var dto = new DocumentDetailDto
        {
            Type = "SALES_ORDER",
            Number = "SO-100",
            Date = System.DateTime.Today,
            PartnerId = partner.Id,
            Currency = "TRY",
            Lines = new List<Application.Documents.DTOs.DocumentLineDto>
            {
                new Application.Documents.DTOs.DocumentLineDto { ItemId = prod.Id, Qty = 2, UnitPrice = 10m, Uom = "ADET", VatRate = 10 },
                new Application.Documents.DTOs.DocumentLineDto { ItemId = prod.Id, Qty = 3, UnitPrice = 5m, Uom = "ADET", VatRate = 10 }
            }
        };

        var soId = await svc.CreateDraftAsync(dto);

        // Approve the sales order
        await svc.ApproveAsync(soId);

        // Act: convert to dispatch
        var newId = await svc.ConvertSalesOrderToDispatchAsync(soId);

        // Assert: new document exists and is draft dispatch with copied lines
        var dst = await Ctx.Documents.FindAsync(newId);
        Assert.NotNull(dst);
        Assert.Equal(DocumentType.SEVK_IRSALIYESI, dst.Type);
        Assert.Equal(DocumentStatus.DRAFT, dst.Status);

        var dstLines = Ctx.DocumentLines.Where(l => l.DocumentId == newId).ToList();
        var srcLines = Ctx.DocumentLines.Where(l => l.DocumentId == soId).ToList();
        Assert.Equal(srcLines.Count, dstLines.Count);
        for (int i = 0; i < srcLines.Count; i++)
        {
            Assert.Equal(srcLines[i].Qty, dstLines[i].Qty);
            Assert.Equal(srcLines[i].UnitPrice, dstLines[i].UnitPrice);
            Assert.Equal(srcLines[i].VatRate, dstLines[i].VatRate);
            Assert.Equal(srcLines[i].Uom, dstLines[i].Uom);
        }
    }
}
