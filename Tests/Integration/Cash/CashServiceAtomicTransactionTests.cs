using InventoryERP.Application.Cash.DTOs;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using InventoryERP.Infrastructure.Services;
using Tests.Infrastructure;
using Xunit;

namespace InventoryERP.Tests.Integration.Cash;

public class CashServiceAtomicTransactionTests : BaseIntegrationTest
{
    [Fact]
    public async Task CreateReceiptAsync_WritesLedgerAndUpdatesBalance()
    {
        // Seed cash account
        var account = new CashAccount { Name = "Merkez Kasa", Type = CashAccountType.Cash, Currency = "TRY", IsActive = true };
        Ctx.CashAccounts.Add(account);
        await Ctx.SaveChangesAsync();

        var svc = new CashService(Ctx);
        var dto = new CashReceiptDto
        {
            CashAccountId = account.Id,
            Date = DateTime.Today,
            Amount = 400m,
            Currency = "TRY",
            FxRate = 1m,
            Description = "R-185 Tahsilat"
        };

        var entryId = await svc.CreateReceiptAsync(dto);

        // Assert latest entry and running balance
        var latest = Ctx.CashLedgerEntries
            .Where(e => e.CashAccountId == account.Id)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .First();
        Assert.Equal(entryId, latest.Id);
        Assert.Equal(400m, latest.Debit);
        Assert.Equal(0m, latest.Credit);
        Assert.Equal(400m, latest.Balance);
        Assert.Equal(LedgerStatus.OPEN, latest.Status);
        Assert.Equal(DocumentType.RECEIPT, latest.DocType);
    }
}
