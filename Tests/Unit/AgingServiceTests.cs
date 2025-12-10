using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Tests.Infrastructure;
using Xunit;
using Microsoft.EntityFrameworkCore;

public class AgingServiceTests : BaseIntegrationTest
{
    [Fact]
    public async Task Buckets_CorrectlyCalculated_ForVariousDueDates()
    {
        var today = new DateTime(2025, 10, 26);
        var partner = new Partner { Title = "Test", Role = PartnerRole.CUSTOMER };
        Ctx.Partners.Add(partner);
        Ctx.SaveChanges();
        var doc = new Document { Type = DocumentType.SALES_INVOICE, Date = today, Status = DocumentStatus.APPROVED, PartnerId = partner.Id, Currency = "TRY", TotalTry = 1500 };
            Ctx.Documents.Add(doc);
            Ctx.SaveChanges();
        var entries = new List<PartnerLedgerEntry>
        {
                new() { PartnerId = partner.Id, DocId = doc.Id, Date = today.AddDays(-1), DueDate = today.AddDays(10), Debit = 100, Credit = 0, AmountTry = 100, Status = LedgerStatus.OPEN }, // NotDue
                new() { PartnerId = partner.Id, DocId = doc.Id, Date = today.AddDays(-31), DueDate = today.AddDays(-10), Debit = 200, Credit = 0, AmountTry = 200, Status = LedgerStatus.OPEN }, // 0-30
                new() { PartnerId = partner.Id, DocId = doc.Id, Date = today.AddDays(-61), DueDate = today.AddDays(-40), Debit = 300, Credit = 0, AmountTry = 300, Status = LedgerStatus.OPEN }, // 31-60
                new() { PartnerId = partner.Id, DocId = doc.Id, Date = today.AddDays(-91), DueDate = today.AddDays(-70), Debit = 400, Credit = 0, AmountTry = 400, Status = LedgerStatus.OPEN }, // 61-90
                new() { PartnerId = partner.Id, DocId = doc.Id, Date = today.AddDays(-120), DueDate = today.AddDays(-100), Debit = 500, Credit = 0, AmountTry = 500, Status = LedgerStatus.OPEN }, // 90+
        };
        Ctx.PartnerLedgerEntries.AddRange(entries);
        Ctx.SaveChanges();
        var service = new AgingService(Ctx);
        var result = await service.GetPartnerAgingAsync(partner.Id, today);
        Assert.Equal(5, result.Buckets.Count);
        Assert.Equal(100, result.Buckets["NotDue"]);
        Assert.Equal(200, result.Buckets["0-30"]);
        Assert.Equal(300, result.Buckets["31-60"]);
        Assert.Equal(400, result.Buckets["61-90"]);
        Assert.Equal(500, result.Buckets["90+"]);
        Assert.Equal(1500, result.Total);
    }
}
