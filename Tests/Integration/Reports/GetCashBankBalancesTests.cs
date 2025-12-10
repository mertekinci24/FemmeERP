using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using InventoryERP.Domain.Entities;
using InventoryERP.Infrastructure.Queries;
using Tests.Infrastructure;

namespace Tests.Integration.Reports;

public class GetCashBankBalancesTests : BaseIntegrationTest
{
    [Fact]
    public async Task DashboardQueries_Returns_CashBankBalances()
    {
        // arrange
    var kasa = new Partner { PartnerType = Domain.Enums.PartnerType.Other, Name = "KasaMain", TaxId = "1111111111" };
    var banka = new Partner { PartnerType = Domain.Enums.PartnerType.Other, Name = "BankaMain", TaxId = "2222222222" };
        Ctx.Partners.Add(kasa);
        Ctx.Partners.Add(banka);
        await Ctx.SaveChangesAsync();

        // create documents referenced by ledger entries to satisfy FK
        var d1 = new Document { Type = Domain.Enums.DocumentType.RCPT_CUSTOMER, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,10,1) };
    var d2 = new Document { Type = Domain.Enums.DocumentType.RCPT_CUSTOMER, Status = Domain.Enums.DocumentStatus.POSTED, Date = new DateTime(2025,10,2) };
        Ctx.Documents.Add(d1);
        Ctx.Documents.Add(d2);
        await Ctx.SaveChangesAsync();

        // kasa: two receipts totaling 200
        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = kasa.Id, DocId = d1.Id, Date = new DateTime(2025,10,1), Debit = 0m, Credit = 100m, AmountTry = 100m, Status = Domain.Enums.LedgerStatus.OPEN });
        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = kasa.Id, DocId = d2.Id, Date = new DateTime(2025,10,2), Debit = 0m, Credit = 100m, AmountTry = 100m, Status = Domain.Enums.LedgerStatus.OPEN });

        // banka: one deposit 500 and one withdrawal -150
        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = banka.Id, DocId = d1.Id, Date = new DateTime(2025,10,1), Debit = 0m, Credit = 500m, AmountTry = 500m, Status = Domain.Enums.LedgerStatus.OPEN });
        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry { PartnerId = banka.Id, DocId = d2.Id, Date = new DateTime(2025,10,3), Debit = 150m, Credit = 0m, AmountTry = -150m, Status = Domain.Enums.LedgerStatus.OPEN });

        await Ctx.SaveChangesAsync();

        var svc = new DashboardQueriesEf(Ctx);

        // act
        var res = await svc.GetCashBankBalancesAsync(new DateTime(2025,10,31));

        // assert
        res.Should().NotBeNull();
        res.TotalCashBankBalanceTry.Should().Be(550m); // 200 + (500 - 150)
        res.Accounts.Should().ContainSingle(a => a.Name == "KasaMain");
        res.Accounts.Should().ContainSingle(a => a.Name == "BankaMain");
        var k = res.Accounts.Find(a => a.Name == "KasaMain");
        k.BalanceTry.Should().Be(200m);
        var b = res.Accounts.Find(a => a.Name == "BankaMain");
        b.BalanceTry.Should().Be(350m);
    }
}
