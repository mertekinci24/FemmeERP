using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using InventoryERP.Presentation.ViewModels.Cash;

namespace Tests.Unit;

public class CashPaymentDialogViewModelTests
{
    [Fact]
    public async Task Save_rejects_no_account_and_zero_amount()
    {
        var fake = new FakeCashService();
        var vm = new CashPaymentDialogViewModel(fake);
        await Task.Delay(10);
        vm.SelectedCashAccount = null; // no account
        vm.Amount = 0m;

        vm.SaveCommand.Execute(null);
        Assert.False(vm.DialogResult);
        Assert.Empty(fake.Payments);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task Save_with_valid_input_invokes_service()
    {
        var fake = new FakeCashService();
        var vm = new CashPaymentDialogViewModel(fake);
        await Task.Delay(10);
        vm.SelectedCashAccount = fake.Accounts.First();
        vm.Amount = 10m;
        vm.Description = string.Empty; // allowed

        vm.SaveCommand.Execute(null);
        Assert.True(vm.DialogResult);
        Assert.Single(fake.Payments);
    }
}

