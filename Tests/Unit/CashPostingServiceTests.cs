using System.Threading.Tasks;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Application.Cash.DTOs;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Tests.Infrastructure;
using Xunit;

public class CashPostingServiceTests : BaseIntegrationTest
{
    [Fact]
    public async Task CreatePaymentAsync_WritesCreditAndReducesBalance()
    {
        var account = new CashAccount { Name = "Kasa", Type = CashAccountType.Cash, Currency = "TRY", IsActive = true };
        Ctx.CashAccounts.Add(account);
        await Ctx.SaveChangesAsync();

        var svc = new CashService(Ctx);
        // First a receipt to have positive balance
        await svc.CreateReceiptAsync(new CashReceiptDto { CashAccountId = account.Id, Date = System.DateTime.Today, Amount = 500m, Currency = "TRY", FxRate = 1m, Description = "ilk" });
        // Then a payment
        var payId = await svc.CreatePaymentAsync(new CashPaymentDto { CashAccountId = account.Id, Date = System.DateTime.Today, Amount = 200m, Currency = "TRY", FxRate = 1m, Description = "odeme" });

        var last = Ctx.CashLedgerEntries.Find(payId);
        Assert.NotNull(last);
        Assert.Equal(0m, last!.Debit);
        Assert.Equal(200m, last.Credit);
        Assert.Equal(300m, last.Balance);
        Assert.Equal(DocumentType.PAYMENT, last.DocType);
    }
}
