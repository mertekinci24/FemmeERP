using System.Threading.Tasks;
using Xunit;
using Tests.Infrastructure;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.Integration;

public class DocumentsMultiUomStockMoveTests : BaseIntegrationTest
{
    [Fact]
    public async Task Sevk_With_KOLI_Line_Creates_StockMove_In_BASE_UOM()
    {
        // Arrange
        var p = new Partner { Title = "CustUOM", Role = Domain.Enums.PartnerRole.CUSTOMER };
        Ctx.Partners.Add(p);
        var prod = new Product { Sku = "PCK", Name = "PackProd", BaseUom = "ADET", VatRate = 10, ReservedQty = 0m };
        Ctx.Products.Add(prod);
        await Ctx.SaveChangesAsync();

        // add ProductUom KOLI -> 12
        var pu = new ProductUom { ProductId = prod.Id, UomName = "KOLI", Coefficient = 12m };
        Ctx.Add(pu);
        await Ctx.SaveChangesAsync();

        var docSvc = new global::InventoryERP.Infrastructure.Services.DocumentCommandService(Ctx, new InventoryERP.Persistence.Services.InventoryQueriesEf(Ctx));
        var dto = new Application.Documents.DTOs.DocumentDetailDto { Type = "SEVK_IRSALIYESI", Number = "S1", Date = System.DateTime.Today, PartnerId = p.Id };
        dto.Lines.Add(new Application.Documents.DTOs.DocumentLineDto { ItemId = prod.Id, Qty = 2m, UnitPrice = 0m, Uom = "KOLI", VatRate = 10, Coefficient = 12m });

        var id = await docSvc.CreateDraftAsync(dto);
        // Approve will create StockMove
        await docSvc.ApproveAsync(id);

        var sm = await Ctx.StockMoves.FirstOrDefaultAsync(s => s.DocLineId != 0);
        Assert.NotNull(sm);
        // expected base qty = 2 * 12 = 24, outgoing so negative
        Assert.Equal(-24m, sm!.QtySigned);
    }
}
