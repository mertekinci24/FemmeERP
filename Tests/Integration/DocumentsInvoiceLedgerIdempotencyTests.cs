using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;
using Moq;
using InventoryERP.Domain.Interfaces;

namespace Tests.Integration
{
    public class DocumentsInvoiceLedgerIdempotencyTests : BaseIntegrationTest
    {
        [Fact]
        public async Task SalesInvoice_Posting_Is_Idempotent_For_LedgerEntries_And_Does_Not_Create_StockMoves()
        {
            var db = Ctx;

            // ensure required master data exists
            db.Partners.Add(new Partner { Role = PartnerRole.CUSTOMER, Title = "InvP" });
            db.Products.Add(new Product { Sku = "SKU-INV", Name = "InvProd", BaseUom = "pcs", VatRate = 20 });
            await db.SaveChangesAsync();

            // create a sales invoice draft with one line
            var doc = new Document
            {
                Type = DocumentType.SALES_INVOICE,
                Status = DocumentStatus.DRAFT,
                Number = "SI-100",
                PartnerId = 1,
            };
            var line = new DocumentLine
            {
                ItemId = 1,
                Qty = 2m,
                UnitPrice = 50m,
                Uom = "pcs",
                VatRate = 20,
            };
            doc.Lines.Add(line);
            db.Documents.Add(doc);
            await db.SaveChangesAsync();

            var postingSvc = new InvoicePostingService(db, new InventoryERP.Persistence.Services.InventoryQueriesEf(db));

            // First approve/post
            await postingSvc.ApproveAndPostAsync(doc.Id, null, doc.Number, CancellationToken.None);

            var ledgerAfterFirst = await db.PartnerLedgerEntries.CountAsync(le => le.DocId == doc.Id && le.DocType == doc.Type);
            var movesAfterFirst = await db.StockMoves.CountAsync(sm => sm.DocLineId == line.Id);

            ledgerAfterFirst.Should().BeGreaterThan(0, "ledger entries should be created for a sales invoice");
            movesAfterFirst.Should().Be(0, "sales invoice should not create stock moves after R-027 refactor");

            // Second approve/post (idempotency)
            await postingSvc.ApproveAndPostAsync(doc.Id, null, doc.Number, CancellationToken.None);

            var ledgerAfterSecond = await db.PartnerLedgerEntries.CountAsync(le => le.DocId == doc.Id && le.DocType == doc.Type);
            var movesAfterSecond = await db.StockMoves.CountAsync(sm => sm.DocLineId == line.Id);

            ledgerAfterSecond.Should().Be(ledgerAfterFirst, "re-posting should not create duplicate ledger entries");
            movesAfterSecond.Should().Be(0, "no stock moves should be created for sales invoice on re-post");
        }
    }
}
