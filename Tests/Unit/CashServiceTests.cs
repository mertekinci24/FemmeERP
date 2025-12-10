using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using InventoryERP.Application.Cash;
using InventoryERP.Application.Cash.DTOs;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Tests.Infrastructure;

namespace Tests.Unit;

public class CashServiceTests
{
    [Fact]
    public async Task Running_balances_update_across_dates_and_order()
    {
        var (ctx, conn) = TestDbContextFactory.Create();
        try
        {
            var svc = new CashService(ctx);

            // create account
            var accId = await svc.CreateAccountAsync(new CashAccountDto { Name = "Kasa", Currency = "TRY", IsActive = true });

            // add receipt on 5th
            await svc.CreateReceiptAsync(new CashReceiptDto { CashAccountId = accId, Date = new DateTime(2025, 1, 5), Amount = 100m });
            // add payment on 3rd (out of order)
            await svc.CreatePaymentAsync(new CashPaymentDto { CashAccountId = accId, Date = new DateTime(2025, 1, 3), Amount = 40m });
            // add receipt on 7th
            await svc.CreateReceiptAsync(new CashReceiptDto { CashAccountId = accId, Date = new DateTime(2025, 1, 7), Amount = 25m });

            var entries = await svc.GetLedgerEntriesAsync(accId);
            Assert.Equal(3, entries.Count);

            // ordered by date,id: 3rd payment, 5th receipt, 7th receipt
            Assert.Equal(new DateTime(2025,1,3), entries[0].Date);
            Assert.Equal(-40m, entries[0].Debit - entries[0].Credit);
            Assert.Equal(-40m, entries[0].Balance);

            Assert.Equal(new DateTime(2025,1,5), entries[1].Date);
            Assert.Equal(60m, entries[1].Balance); // -40 + 100

            Assert.Equal(new DateTime(2025,1,7), entries[2].Date);
            Assert.Equal(85m, entries[2].Balance); // 60 + 25

            var asOf4th = await svc.GetBalanceAsync(accId, new DateTime(2025,1,4));
            Assert.Equal(-40m, asOf4th);
            var asOf6th = await svc.GetBalanceAsync(accId, new DateTime(2025,1,6));
            Assert.Equal(60m, asOf6th);
            var final = await svc.GetBalanceAsync(accId);
            Assert.Equal(85m, final);
        }
        finally
        {
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Error_on_nonexistent_account_for_ops()
    {
        var (ctx, conn) = TestDbContextFactory.Create();
        try
        {
            var svc = new CashService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await svc.CreateReceiptAsync(new CashReceiptDto { CashAccountId = 999, Date = DateTime.Today, Amount = 10m }));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await svc.CreatePaymentAsync(new CashPaymentDto { CashAccountId = 999, Date = DateTime.Today, Amount = 10m }));
        }
        finally
        {
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Delete_account_with_entries_throws()
    {
        var (ctx, conn) = TestDbContextFactory.Create();
        try
        {
            var svc = new CashService(ctx);
            var accId = await svc.CreateAccountAsync(new CashAccountDto { Name = "Kasa", IsActive = true });
            await svc.CreateReceiptAsync(new CashReceiptDto { CashAccountId = accId, Amount = 50m, Date = DateTime.Today });
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteAccountAsync(accId));
        }
        finally { conn.Dispose(); }
    }

    [Fact]
    public async Task Get_ledger_entries_respects_date_range()
    {
        var (ctx, conn) = TestDbContextFactory.Create();
        try
        {
            var svc = new CashService(ctx);
            var accId = await svc.CreateAccountAsync(new CashAccountDto { Name = "Kasa", IsActive = true });
            await svc.CreateReceiptAsync(new CashReceiptDto { CashAccountId = accId, Amount = 10m, Date = new DateTime(2025,1,1) });
            await svc.CreateReceiptAsync(new CashReceiptDto { CashAccountId = accId, Amount = 20m, Date = new DateTime(2025,1,10) });
            await svc.CreatePaymentAsync(new CashPaymentDto { CashAccountId = accId, Amount = 5m, Date = new DateTime(2025,1,20) });

            var range = await svc.GetLedgerEntriesAsync(accId, new DateTime(2025,1,5), new DateTime(2025,1,15));
            Assert.Equal(1, range.Count); // only 10th
            Assert.Equal(new DateTime(2025,1,10), range[0].Date);
        }
        finally { conn.Dispose(); }
    }
}
