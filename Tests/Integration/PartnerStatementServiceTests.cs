using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Queries;
using InventoryERP.Application.Partners;
using Tests.Infrastructure;

namespace Tests.Integration;

public class PartnerStatementServiceTests : BaseIntegrationTest
{
    [Fact]
    public async Task BuildAsync_ComputesRunningBalance_And_ExcludesCanceled()
    {
        // arrange
        var partner = new Partner { Title = "P-1" };
        Ctx.Partners.Add(partner);
        await Ctx.SaveChangesAsync();

    // create a document to satisfy required DocId FK
    var doc = new Domain.Entities.Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Date = DateTime.Today.AddDays(-6), Number = "INV-1", Status = Domain.Enums.DocumentStatus.POSTED };
    Ctx.Documents.Add(doc);
    await Ctx.SaveChangesAsync();

    // entries: open, closed, canceled (DocId required)
    var e1 = new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = DateTime.Today.AddDays(-5), Debit = 100m, Credit = 0m, AmountTry = 100m, Status = LedgerStatus.OPEN };
    var e2 = new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = DateTime.Today.AddDays(-4), Debit = 0m, Credit = 20m, AmountTry = -20m, Status = LedgerStatus.CLOSED };
    var e3 = new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, Date = DateTime.Today.AddDays(-3), Debit = 50m, Credit = 0m, AmountTry = 50m, Status = LedgerStatus.CANCELED };

    Ctx.PartnerLedgerEntries.AddRange(e1, e2, e3);
        await Ctx.SaveChangesAsync();

        var svc = new PartnerReadService(Ctx);

        // act
        var dto = await svc.BuildStatementAsync(partner.Id, null, null);

        // expected: running computed over rows in date order but should exclude canceled entries per business rule
        var expectedRows = Ctx.PartnerLedgerEntries.Where(x => x.PartnerId == partner.Id && x.Status != LedgerStatus.CANCELED).OrderBy(x => x.Date).ToList();

        dto.Rows.Count.Should().Be(expectedRows.Count);
        // compute running
        decimal running = 0m;
        for (int i = 0; i < expectedRows.Count; i++)
        {
            var r = expectedRows[i];
            running += r.Debit - r.Credit;
            dto.Rows[i].BalanceAfter.Should().Be(running);
        }
    }

    [Fact(DisplayName = "TST-023 Cari Hareket Sekmesi Net 600 BORÇ")]
    public async Task TST023_LedgerReflectsInvoiceAndReceiptNetBalance()
    {
        var partner = new Partner { Title = "Cari ABC" };
        Ctx.Partners.Add(partner);
        await Ctx.SaveChangesAsync();

        var invoiceDoc = new Document
        {
            Type = DocumentType.SALES_INVOICE,
            Date = DateTime.Today.AddDays(-2),
            Number = "INV-600",
            Status = DocumentStatus.POSTED
        };

        var receiptDoc = new Document
        {
            Type = DocumentType.RECEIPT,
            Date = DateTime.Today.AddDays(-1),
            Number = "RCPT-400",
            Status = DocumentStatus.POSTED
        };

        Ctx.Documents.AddRange(invoiceDoc, receiptDoc);
        await Ctx.SaveChangesAsync();

        var debitEntry = new PartnerLedgerEntry
        {
            PartnerId = partner.Id,
            DocId = invoiceDoc.Id,
            DocType = invoiceDoc.Type,
            DocNumber = invoiceDoc.Number,
            Date = invoiceDoc.Date,
            Debit = 1000m,
            Credit = 0m,
            AmountTry = 1000m,
            Status = LedgerStatus.OPEN
        };

        var creditEntry = new PartnerLedgerEntry
        {
            PartnerId = partner.Id,
            DocId = receiptDoc.Id,
            DocType = receiptDoc.Type,
            DocNumber = receiptDoc.Number,
            Date = receiptDoc.Date,
            Debit = 0m,
            Credit = 400m,
            AmountTry = -400m,
            Status = LedgerStatus.CLOSED
        };

        Ctx.PartnerLedgerEntries.AddRange(debitEntry, creditEntry);
        await Ctx.SaveChangesAsync();

        var svc = new PartnerReadService(Ctx);

        var dto = await svc.BuildStatementAsync(partner.Id, null, null);

        dto.Rows.Should().HaveCount(2);
        dto.TotalDebit.Should().Be(1000m);
        dto.TotalCredit.Should().Be(400m);
        dto.EndingBalance.Should().Be(600m);
        dto.Rows.Last().BalanceAfter.Should().Be(600m);
    }
}
