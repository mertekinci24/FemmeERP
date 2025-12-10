using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using FluentAssertions;
using InventoryERP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;
using Moq;
using InventoryERP.Domain.Interfaces;
using Xunit;

namespace Tests.Integration;

public class ProductionPostingTests : BaseIntegrationTest
{
    [Fact]
    public async Task Production_Posting_Adds_FG_And_Consumes_Components()
    {
        var db = Ctx;
        // master data
        var wh = new Warehouse { Code = "WH1", Name = "Main" };
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync();
        var locSrc = new Location { WarehouseId = wh.Id, Code = "SRC", Name = "Source" };
        var locDst = new Location { WarehouseId = wh.Id, Code = "DST", Name = "Dest" };
        db.Locations.AddRange(locSrc, locDst);
        var fg = new Product { Sku = "FG1", Name = "Finished", BaseUom = "pcs", VatRate = 1 };
        var c1 = new Product { Sku = "C1", Name = "Comp1", BaseUom = "pcs", VatRate = 1 };
        var c2 = new Product { Sku = "C2", Name = "Comp2", BaseUom = "pcs", VatRate = 1 };
        db.Products.AddRange(fg, c1, c2);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem { ParentProductId = fg.Id, ComponentProductId = c1.Id, QtyPer = 2m });
        db.BomItems.Add(new BomItem { ParentProductId = fg.Id, ComponentProductId = c2.Id, QtyPer = 3m });
        await db.SaveChangesAsync();

        // create production doc
        var doc = new Document { Type = DocumentType.URETIM_FISI, Date = System.DateTime.Today, Status = DocumentStatus.DRAFT };
        var line = new DocumentLine { ItemId = fg.Id, Qty = 10m, Uom = "pcs", VatRate = 1, SourceLocationId = locSrc.Id, DestinationLocationId = locDst.Id };
        doc.Lines.Add(line);
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var svc = new InvoicePostingService(db, new InventoryERP.Persistence.Services.InventoryQueriesEf(db));
        await svc.ApproveAndPostAsync(doc.Id, null, null, CancellationToken.None);

        var moves = await db.StockMoves.Where(m => m.DocLineId == line.Id).ToListAsync();
        moves.Should().HaveCount(3);
        moves.Should().Contain(m => m.ItemId == fg.Id && m.QtySigned == 10m && m.DestinationLocationId == locDst.Id);
        moves.Should().Contain(m => m.ItemId == c1.Id && m.QtySigned == -20m && m.SourceLocationId == locSrc.Id);
        moves.Should().Contain(m => m.ItemId == c2.Id && m.QtySigned == -30m && m.SourceLocationId == locSrc.Id);

        // No partner ledger for production
        var ledgerCount = await db.PartnerLedgerEntries.CountAsync(le => le.DocId == doc.Id);
        ledgerCount.Should().Be(0);
    }
}
