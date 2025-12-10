using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;

namespace Tests.Integration;

public class PartnerLedgerConstraintsTests : BaseIntegrationTest
{
    [Fact]
    public void BothDebitAndCredit_NonZero_Should_Fail()
    {
        var p = new Partner { Role = PartnerRole.CUSTOMER, Title = "X" };
        Ctx.Partners.Add(p);
        Ctx.SaveChanges();

        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry
        {
            PartnerId = p.Id,
            Date = DateTime.UtcNow,
            Currency = "TRY",
            FxRate = 1,
            Debit = 100,
            Credit = 50,
            AmountTry = 150,
            Status = LedgerStatus.OPEN
        });

        Action act = () => Ctx.SaveChanges();
        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void BothDebitAndCredit_Zero_Should_Fail()
    {
        var p = new Partner { Role = PartnerRole.CUSTOMER, Title = "Y" };
        Ctx.Partners.Add(p);
        Ctx.SaveChanges();

        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry
        {
            PartnerId = p.Id,
            Date = DateTime.UtcNow,
            Currency = "TRY",
            FxRate = 1,
            Debit = 0,
            Credit = 0,
            AmountTry = 0,
            Status = LedgerStatus.OPEN
        });

        Action act = () => Ctx.SaveChanges();
        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void OnlyDebitOrOnlyCredit_Should_Pass()
    {
        var p = new Partner { Role = PartnerRole.SUPPLIER, Title = "Z" };
        Ctx.Partners.Add(p);
        Ctx.SaveChanges();

        var d = new Document { Type = DocumentType.SALES_INVOICE, Number = "OK1", Date = DateTime.UtcNow, Status = DocumentStatus.POSTED, Currency = "TRY", FxRate = 1 };
        Ctx.Documents.Add(d);
        Ctx.SaveChanges();

        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry
        {
            PartnerId = p.Id,
            DocId = d.Id,
            Date = DateTime.UtcNow,
            Currency = "TRY",
            FxRate = 1,
            Debit = 200,
            Credit = 0,
            AmountTry = 200,
            Status = LedgerStatus.OPEN
        });

        Ctx.SaveChanges();

        var d2 = new Document { Type = DocumentType.SALES_INVOICE, Number = "OK2", Date = DateTime.UtcNow, Status = DocumentStatus.POSTED, Currency = "TRY", FxRate = 1 };
        Ctx.Documents.Add(d2);
        Ctx.SaveChanges();

        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry
        {
            PartnerId = p.Id,
            DocId = d2.Id,
            Date = DateTime.UtcNow,
            Currency = "TRY",
            FxRate = 1,
            Debit = 0,
            Credit = 150,
            AmountTry = 150,
            Status = LedgerStatus.OPEN
        });

        Ctx.SaveChanges();
    }

    [Fact]
    public void BothZero_Should_Fail()
    {
        var p = new Partner { Role = PartnerRole.CUSTOMER, Title = "W" };
        Ctx.Partners.Add(p);
        Ctx.SaveChanges();

        Ctx.PartnerLedgerEntries.Add(new PartnerLedgerEntry
        {
            PartnerId = p.Id,
            Date = DateTime.UtcNow,
            Currency = "TRY",
            FxRate = 1,
            Debit = 0,
            Credit = 0,
            AmountTry = 0,
            Status = LedgerStatus.OPEN
        });

        Action act = () => Ctx.SaveChanges();
        act.Should().Throw<DbUpdateException>();
    }
}
