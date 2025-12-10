using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;

namespace Tests.Integration;

public class FkCascadeRestrictTests : BaseIntegrationTest
{
    [Fact]
    public void DeleteDocument_CascadesToLines()
    {
        var doc = new Document { Type = DocumentType.SALES_INVOICE, Number = "D1", Date = DateTime.UtcNow, Status = DocumentStatus.POSTED, Currency = "TRY", FxRate = 1m };
        var p = new Product { Sku = Guid.NewGuid().ToString(), Name = "X", BaseUom = "EA", VatRate = 20 };
        Ctx.Products.Add(p); Ctx.SaveChanges();
        var line = new DocumentLine { Document = doc, ItemId = p.Id, Qty = 1, Uom = "EA", UnitPrice = 10, VatRate = 20 };
        Ctx.DocumentLines.Add(line);
        Ctx.SaveChanges();

        Ctx.Documents.Remove(doc);
        Ctx.SaveChanges();

        Ctx.DocumentLines.Any(e => e.Id == line.Id).Should().BeFalse();
    }

    [Fact]
    public void DeleteDocument_WithLedgerEntry_Should_Restrict()
    {
        var doc = new Document { Type = DocumentType.SALES_INVOICE, Number = "D2", Date = DateTime.UtcNow, Status = DocumentStatus.POSTED, Currency = "TRY", FxRate = 1m };
        Ctx.Documents.Add(doc);
        var partner = new Partner { Role = PartnerRole.CUSTOMER, Title = "R1" };
        Ctx.Partners.Add(partner);
        Ctx.SaveChanges();

        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry {
            PartnerId = partner.Id, DocId = doc.Id, Date = DateTime.UtcNow, Currency = "TRY", FxRate = 1, Debit = 100, Credit = 0, AmountTry = 100, Status = LedgerStatus.OPEN
        });
        Ctx.SaveChanges();

    Action act = () => { Ctx.Documents.Remove(doc); Ctx.SaveChanges(); };
    act.Should().Throw<InvalidOperationException>();
    }
}
