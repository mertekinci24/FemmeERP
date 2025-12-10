using InventoryERP.Domain.Entities;
using FluentAssertions;
using Tests.Infrastructure;

namespace Tests.Unit;

public class RoundingTests : BaseIntegrationTest
{
    [Fact]
    public void Money_And_Quantity_Rounded_On_Save()
    {
        var p = new Domain.Entities.Product { Sku = Guid.NewGuid().ToString(), Name = "X", BaseUom = "EA", VatRate = 20 };
        Ctx.Products.Add(p);
        var partner = new Domain.Entities.Partner { Role = Domain.Enums.PartnerRole.CUSTOMER, Title = "Rnd" };
        Ctx.Partners.Add(partner);
        var doc = new Domain.Entities.Document{ Type=Domain.Enums.DocumentType.SALES_INVOICE, Number="RND", Date=DateTime.UtcNow, Status=Domain.Enums.DocumentStatus.POSTED, Currency="TRY", FxRate=1};
        Ctx.Documents.Add(doc);
        Ctx.SaveChanges();
        var ple = new Domain.Entities.PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = DateTime.UtcNow, Currency = "TRY", FxRate = 1.2345678m, Debit = 2.345m, Credit = 0m, AmountTry = 2.345m, Status = Domain.Enums.LedgerStatus.OPEN };
        Ctx.PartnerLedgerEntries.Add(ple);
        var dl = new Domain.Entities.DocumentLine { Document = doc, ItemId = p.Id, Qty = 1.2345m, Uom = "EA", UnitPrice = 9.87654321m, VatRate = 20 };
        Ctx.DocumentLines.Add(dl);
        Ctx.StockMoves.Add(new Domain.Entities.StockMove { ItemId = p.Id, Date = DateTime.UtcNow, QtySigned = 3.14159m, UnitCost = 7.6543219m, DocLineId=0, DocumentLine = dl });
        Ctx.SaveChanges();

        ple.FxRate.Should().Be(1.234568m);
        ple.Debit.Should().Be(2.35m);
        ple.AmountTry.Should().Be(2.35m);
        dl.Qty.Should().Be(1.235m);
        dl.UnitPrice.Should().Be(9.876543m);
    }
}
