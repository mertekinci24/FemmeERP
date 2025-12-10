using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Queries;
using Tests.Infrastructure;

namespace Tests.Integration;

public class PartnerAgingServiceTests : BaseIntegrationTest
{
    [Fact]
    public async Task BuildAsync_BucketsAmounts_ByDueDateAndStatus()
    {
        // arrange
        var partner = new Partner { Title = "Aging P" };
        Ctx.Partners.Add(partner);
        await Ctx.SaveChangesAsync();

    var asOf = DateOnly.FromDateTime(DateTime.Today);

    var doc = new Domain.Entities.Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Date = DateTime.Today.AddDays(-10), Number = "INV-A", Status = Domain.Enums.DocumentStatus.POSTED };
    Ctx.Documents.Add(doc);
    await Ctx.SaveChangesAsync();

    // create entries with DueDates to fall into buckets (DocId required)
    Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, DueDate = DateTime.Today.AddDays(-5), Debit = 100m, Credit = 0m, Status = LedgerStatus.OPEN }); // 0-30
    Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, DueDate = DateTime.Today.AddDays(-35), Debit = 50m, Credit = 0m, Status = LedgerStatus.OPEN }); // 31-60
    Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, DueDate = DateTime.Today.AddDays(-65), Debit = 30m, Credit = 0m, Status = LedgerStatus.OPEN }); // 61-90
    Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, DueDate = DateTime.Today.AddDays(-200), Debit = 10m, Credit = 0m, Status = LedgerStatus.OPEN }); // 90+

    // canceled or closed should be ignored
    Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = partner.Id, DocId = doc.Id, DueDate = DateTime.Today.AddDays(-5), Debit = 500m, Credit = 0m, Status = LedgerStatus.CANCELED });

    await Ctx.SaveChangesAsync();

        var svc = new PartnerReadService(Ctx);

        // act
        var dto = await svc.BuildAgingAsync(partner.Id, asOf);

        // assert - buckets in order: 0-30,31-60,61-90,90+
        dto.Buckets.Count.Should().Be(4);
        dto.Buckets[0].AmountTry.Should().Be(100m);
        dto.Buckets[1].AmountTry.Should().Be(50m);
        dto.Buckets[2].AmountTry.Should().Be(30m);
        dto.Buckets[3].AmountTry.Should().Be(10m);
        dto.Total.Should().Be(190m);
    }
}
