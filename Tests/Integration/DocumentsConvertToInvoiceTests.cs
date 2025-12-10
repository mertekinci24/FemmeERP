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

public class DocumentsConvertToInvoiceTests : BaseIntegrationTest
{
    [Fact]
    public async Task ConvertApprovedDispatch_CreatesDraftInvoiceWithLines()
    {
        // Arrange: create partner and product
        var partner = new Partner { Title = "CUST2", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        var prod = new Product { Sku = "P2", Name = "Prod2", BaseUom = "ADET", VatRate = 10, ReservedQty = 0m };
        Ctx.Products.Add(prod);
        await Ctx.SaveChangesAsync();

        var svc = new DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));

        // create dispatch draft with two lines
        var dto = new DocumentDetailDto
        {
            Type = "SEVK_IRSALIYESI",
            Number = "DV-200",
            Date = System.DateTime.Today,
            PartnerId = partner.Id,
            Currency = "TRY",
            Lines = new List<Application.Documents.DTOs.DocumentLineDto>
            {
                new Application.Documents.DTOs.DocumentLineDto { ItemId = prod.Id, Qty = 1, UnitPrice = 20m, Uom = "ADET", VatRate = 10 },
                new Application.Documents.DTOs.DocumentLineDto { ItemId = prod.Id, Qty = 4, UnitPrice = 7.5m, Uom = "ADET", VatRate = 10 }
            }
        };

        var dId = await svc.CreateDraftAsync(dto);

        // Approve/post the dispatch
        await svc.ApproveAsync(dId);

        // Act: convert to invoice
        var newId = await svc.ConvertDispatchToInvoiceAsync(dId);

        // Assert: new document exists and is draft invoice with copied lines
        var dst = await Ctx.Documents.FindAsync(newId);
        Assert.NotNull(dst);
        Assert.Equal(DocumentType.SALES_INVOICE, dst.Type);
        Assert.Equal(DocumentStatus.DRAFT, dst.Status);

        var dstLines = Ctx.DocumentLines.Where(l => l.DocumentId == newId).ToList();
        var srcLines = Ctx.DocumentLines.Where(l => l.DocumentId == dId).ToList();
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
