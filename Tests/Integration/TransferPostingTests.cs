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

public class TransferPostingTests : BaseIntegrationTest
{
    [Fact]
    public async Task Transfer_Posting_Creates_InOut_Moves_With_Locations()
    {
        var db = Ctx;
        // master data
        var wh = new Warehouse { Code = "WH1", Name = "Ana Depo" };
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync();
        var locA = new Location { WarehouseId = wh.Id, Code = "A", Name = "Raf A" };
        var locB = new Location { WarehouseId = wh.Id, Code = "B", Name = "Raf B" };
        db.Locations.AddRange(locA, locB);
        db.Products.Add(new Product { Sku = "SKU-T", Name = "Transf", BaseUom = "pcs", VatRate = 20 });
        await db.SaveChangesAsync();

        // draft transfer document
        var doc = new Document { Type = DocumentType.TRANSFER_FISI, Date = System.DateTime.Today, Status = DocumentStatus.DRAFT };
        var line = new DocumentLine { ItemId = db.Products.Single().Id, Qty = 5m, Uom = "pcs", VatRate = 1, SourceLocationId = locA.Id, DestinationLocationId = locB.Id };
        doc.Lines.Add(line);
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var svc = new InvoicePostingService(db, new InventoryERP.Persistence.Services.InventoryQueriesEf(db));
        await svc.ApproveAndPostAsync(doc.Id, null, null, CancellationToken.None);

        var moves = (await db.StockMoves.Where(m => m.DocLineId == line.Id).ToListAsync()).OrderBy(m => m.QtySigned).ToList();
        moves.Should().HaveCount(2);
        moves[0].QtySigned.Should().Be(-5m);
        moves[0].SourceLocationId.Should().Be(locA.Id);
        moves[0].DestinationLocationId.Should().BeNull();
        moves[1].QtySigned.Should().Be(5m);
        moves[1].DestinationLocationId.Should().Be(locB.Id);
        moves[1].SourceLocationId.Should().BeNull();

        // No partner ledger for transfers
        var ledgerCount = await db.PartnerLedgerEntries.CountAsync(le => le.DocId == doc.Id);
        ledgerCount.Should().Be(0);
    }
}
