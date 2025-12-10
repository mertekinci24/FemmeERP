using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Tests.Infrastructure;
using Xunit;
using Moq;
using InventoryERP.Domain.Interfaces;

namespace Tests.Unit;

public class PostingAdjustmentsTests
{
    [Fact]
    public async Task Approve_AdjustmentOut_Creates_Negative_StockMove()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        using var _ = conn; // ensure connection disposed at end
        var db = provider.GetRequiredService<AppDbContext>();

        // Arrange: create a product and draft adjustment OUT document with one line
        var p = new Product { Sku = "SKU-OUT", Name = "Adj Out", BaseUom = "EA", VatRate = 20, Active = true };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var doc = new Document { Type = DocumentType.ADJUSTMENT_OUT, Date = System.DateTime.Today, Status = DocumentStatus.DRAFT, Currency = "TRY" };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();
        var line = new DocumentLine { DocumentId = doc.Id, ItemId = p.Id, Qty = 5m, Uom = "EA", UnitPrice = 0m, VatRate = 20, Coefficient = 1m };
        db.DocumentLines.Add(line);
        await db.SaveChangesAsync();

        var posting = new InvoicePostingService(db, new InventoryERP.Persistence.Services.InventoryQueriesEf(db));

        // Act
        await posting.ApproveAndPostAsync(doc.Id, null, "ADJ-OUT-0001", default);

        // Assert
        var move = db.StockMoves.SingleOrDefault(m => m.DocLineId == line.Id);
        Assert.NotNull(move);
        Assert.Equal(-5m, move!.QtySigned);
    }

    [Fact]
    public async Task Approve_AdjustmentIn_Creates_Positive_StockMove()
    {
        var (provider, conn) = TestServiceProviderFactory.CreateWithInMemoryDb();
        using var _ = conn;
        var db = provider.GetRequiredService<AppDbContext>();

        var p = new Product { Sku = "SKU-IN", Name = "Adj In", BaseUom = "EA", VatRate = 20, Active = true };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var doc = new Document { Type = DocumentType.ADJUSTMENT_IN, Date = System.DateTime.Today, Status = DocumentStatus.DRAFT, Currency = "TRY" };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();
        var line = new DocumentLine { DocumentId = doc.Id, ItemId = p.Id, Qty = 3m, Uom = "EA", UnitPrice = 0m, VatRate = 20, Coefficient = 1m };
        db.DocumentLines.Add(line);
        await db.SaveChangesAsync();

        var posting = new InvoicePostingService(db, new InventoryERP.Persistence.Services.InventoryQueriesEf(db));
        await posting.ApproveAndPostAsync(doc.Id, null, "ADJ-IN-0001", default);

        var move = db.StockMoves.SingleOrDefault(m => m.DocLineId == line.Id);
        Assert.NotNull(move);
        Assert.Equal(+3m, move!.QtySigned);
    }
}
