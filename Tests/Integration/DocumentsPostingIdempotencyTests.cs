using System.Threading;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using FluentAssertions;
using InventoryERP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Tests.Infrastructure;
using InventoryERP.Application.Documents;
using Xunit;
using Moq;
using InventoryERP.Domain.Interfaces;

namespace Tests.Integration
{
    public class DocumentsPostingIdempotencyTests : BaseIntegrationTest
    {
        [Fact]
        public async Task Approve_Is_Idempotent_For_StockMoves_And_LedgerEntries()
        {
            var db = Ctx;

            // ensure required master data exists
            db.Partners.Add(new Partner { Role = PartnerRole.CUSTOMER, Title = "P1" });
            db.Products.Add(new Product { Sku = "SKU1", Name = "Prod 1", BaseUom = "pcs", VatRate = 20 });
            await db.SaveChangesAsync();

            // create a sample draft document with one line
            var doc = new Document
            {
                Type = DocumentType.SEVK_IRSALIYESI,
                Status = DocumentStatus.DRAFT,
                Number = "T-1000",
                PartnerId = 1,
            };
            var line = new DocumentLine
            {
                ItemId = 1,
                Qty = 5m,
                UnitPrice = 10m,
                Uom = "pcs",
                VatRate = 20,
            };
            doc.Lines.Add(line);
            db.Documents.Add(doc);
            await db.SaveChangesAsync();

            var postingSvc = new InvoicePostingService(db, new InventoryERP.Persistence.Services.InventoryQueriesEf(db));

            // Approve (post) first time via posting service
            await postingSvc.ApproveAndPostAsync(doc.Id, null, doc.Number, CancellationToken.None);

            var movesAfterFirst = await db.StockMoves.CountAsync(sm => sm.DocLineId == line.Id);
            var ledgersAfterFirst = await db.PartnerLedgerEntries.CountAsync(le => le.DocId == doc.Id && le.DocType == doc.Type);

            movesAfterFirst.Should().BeGreaterThan(0);
            ledgersAfterFirst.Should().BeGreaterThan(0);

            // Approve second time (should be idempotent)
            await postingSvc.ApproveAndPostAsync(doc.Id, null, doc.Number, CancellationToken.None);

            var movesAfterSecond = await db.StockMoves.CountAsync(sm => sm.DocLineId == line.Id);
            var ledgersAfterSecond = await db.PartnerLedgerEntries.CountAsync(le => le.DocId == doc.Id && le.DocType == doc.Type);

            movesAfterSecond.Should().Be(movesAfterFirst);
            ledgersAfterSecond.Should().Be(ledgersAfterFirst);
        }

        [Fact]
        public async Task Cancel_Is_Idempotent_For_Reverse_Entries()
        {
            var db = Ctx;

            // ensure required master data exists
            db.Partners.Add(new Partner { Role = PartnerRole.CUSTOMER, Title = "P1" });
            db.Products.Add(new Product { Sku = "SKU2", Name = "Prod 2", BaseUom = "pcs", VatRate = 20 });
            await db.SaveChangesAsync();

            // create and post a document
            var doc = new Document
            {
                Type = DocumentType.SEVK_IRSALIYESI,
                Status = DocumentStatus.DRAFT,
                Number = "T-2000",
                PartnerId = 1,
            };
            var line = new DocumentLine
            {
                ItemId = 1,
                Qty = 3m,
                UnitPrice = 15m,
                Uom = "pcs",
                VatRate = 20,
            };
            doc.Lines.Add(line);
            db.Documents.Add(doc);
            await db.SaveChangesAsync();

            var postingSvc = new InvoicePostingService(db, new InventoryERP.Persistence.Services.InventoryQueriesEf(db));
            await postingSvc.ApproveAndPostAsync(doc.Id, null, doc.Number, CancellationToken.None);

            // Now cancel first time
            var reversalSvc = new InvoiceReversalService(db);
            await reversalSvc.ReverseAsync(doc.Id, "test", CancellationToken.None);

            var reverseMovesAfterFirst = await db.StockMoves.CountAsync(sm => sm.DocLineId == line.Id && sm.QtySigned < 0);
            var reverseLedgersAfterFirst = await db.PartnerLedgerEntries.CountAsync(le => le.DocId == doc.Id && le.Status == LedgerStatus.CANCELED);

            reverseMovesAfterFirst.Should().BeGreaterThan(0);
            reverseLedgersAfterFirst.Should().BeGreaterThan(0);

            // Cancel second time (should be idempotent)
            await reversalSvc.ReverseAsync(doc.Id, "test-2", CancellationToken.None);

            var reverseMovesAfterSecond = await db.StockMoves.CountAsync(sm => sm.DocLineId == line.Id && sm.QtySigned < 0);
            var reverseLedgersAfterSecond = await db.PartnerLedgerEntries.CountAsync(le => le.DocId == doc.Id && le.Status == LedgerStatus.CANCELED);

            reverseMovesAfterSecond.Should().Be(reverseMovesAfterFirst);
            reverseLedgersAfterSecond.Should().Be(reverseLedgersAfterFirst);
        }
    }
}
