using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.Queries;
using InventoryERP.Application.Reports;
using Tests.Infrastructure;

namespace Tests.Integration.Reports;

public class GetOverdueReceivablesTests : BaseIntegrationTest
{
    [Fact]
    public async Task DashboardQueries_Returns_Total_And_TopPartners()
    {
        // arrange
        var p1 = new Partner { PartnerType = Domain.Enums.PartnerType.Customer, Name = "Alpha", TaxId = "1234567890" };
        var p2 = new Partner { PartnerType = Domain.Enums.PartnerType.Customer, Name = "Beta", TaxId = "0987654321" };
        Ctx.Partners.Add(p1);
        Ctx.Partners.Add(p2);
        await Ctx.SaveChangesAsync();

        var asOf = new DateTime(2025, 10, 31);

    // create documents referenced by ledger entries
    var doc1 = new Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,9,1) };
    var doc2 = new Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,9,15) };
    var doc3 = new Document { Type = Domain.Enums.DocumentType.SALES_INVOICE, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,8,1) };
    Ctx.Documents.Add(doc1);
    Ctx.Documents.Add(doc2);
    Ctx.Documents.Add(doc3);
    await Ctx.SaveChangesAsync();

    // p1 has two overdue invoices
    Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = p1.Id, DocId = doc1.Id, Date = new DateTime(2025,9,1), DueDate = new DateTime(2025,9,30), Debit = 100m, Credit = 0m, AmountTry = 100m, Status = Domain.Enums.LedgerStatus.OPEN });
    Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = p1.Id, DocId = doc2.Id, Date = new DateTime(2025,9,15), DueDate = new DateTime(2025,10,1), Debit = 50m, Credit = 0m, AmountTry = 50m, Status = Domain.Enums.LedgerStatus.OPEN });

    // p2 has one smaller overdue invoice
    Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = p2.Id, DocId = doc3.Id, Date = new DateTime(2025,8,1), DueDate = new DateTime(2025,8,31), Debit = 75m, Credit = 0m, AmountTry = 75m, Status = Domain.Enums.LedgerStatus.OPEN });

        await Ctx.SaveChangesAsync();

        var svc = new DashboardQueriesEf(Ctx);

        // act
        var res = await svc.GetOverdueReceivablesAsync(asOf, topN: 5);

        // assert
        res.Should().NotBeNull();
        res.TotalOverdueTry.Should().Be(225m);
        res.OverduePartnerCount.Should().Be(2);
    res.TopPartners.Count.Should().BeGreaterThanOrEqualTo(2);
        res.TopPartners[0].Name.Should().Be("Alpha");
        res.TopPartners[0].TotalOverdue.Should().Be(150m);
    }
}
