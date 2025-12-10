using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using InventoryERP.Application.Cash;
using InventoryERP.Application.Cash.DTOs;
using InventoryERP.Presentation.ViewModels.Cash;

namespace Tests.Unit;

class FakeCashService : ICashService
{
    public List<CashReceiptDto> Receipts = new();
    public List<CashPaymentDto> Payments = new();
    public List<CashAccountDto> Accounts { get; } = new()
    {
        new CashAccountDto { Id = 1, Name = "Kasa", Currency = "TRY", IsActive = true }
    };

    public Task<int> CreateAccountAsync(CashAccountDto dto) => Task.FromResult(2);
    public Task DeleteAccountAsync(int id) => Task.CompletedTask;
    public Task<List<CashAccountDto>> GetAllAccountsAsync() => Task.FromResult(Accounts);
    public Task<CashAccountDto?> GetAccountByIdAsync(int id) => Task.FromResult(Accounts.FirstOrDefault(a => a.Id==id));
    public Task<List<CashLedgerDto>> GetLedgerEntriesAsync(int cashAccountId, DateTime? f=null, DateTime? t=null) => Task.FromResult(new List<CashLedgerDto>());
    public Task<decimal> GetBalanceAsync(int cashAccountId, DateTime? asOf=null) => Task.FromResult(0m);
    public Task UpdateAccountAsync(CashAccountDto dto) => Task.CompletedTask;
    public Task<int> CreateReceiptAsync(CashReceiptDto dto){ Receipts.Add(dto); return Task.FromResult(1);}    
    public Task<int> CreatePaymentAsync(CashPaymentDto dto){ Payments.Add(dto); return Task.FromResult(1);}    
}

public class CashReceiptDialogViewModelTests
{
    [Fact]
    public async Task Save_ignores_empty_or_zero_and_sets_error()
    {
        var fake = new FakeCashService();
        var vm = new CashReceiptDialogViewModel(fake);

        // Wait accounts load
        await Task.Delay(10);
        vm.SelectedCashAccount = fake.Accounts.First();
        vm.Amount = 0m; // invalid

        vm.SaveCommand.Execute(null);
        Assert.False(vm.DialogResult);
        Assert.False(fake.Receipts.Any());
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task Save_allows_null_description_and_calls_service()
    {
        var fake = new FakeCashService();
        var vm = new CashReceiptDialogViewModel(fake)
        {
            SelectedCashAccount = fake.Accounts.First(),
            Amount = 123.45m,
            Description = null!
        };
        vm.SaveCommand.Execute(null);
        Assert.True(vm.DialogResult);
        Assert.Single(fake.Receipts);
        Assert.Null(fake.Receipts[0].Description);
    }
}

