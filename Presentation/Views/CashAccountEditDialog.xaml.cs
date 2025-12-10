using System.Windows;
using InventoryERP.Domain.Enums;
using InventoryERP.Presentation.ViewModels.Cash;

namespace InventoryERP.Presentation.Views;

/// <summary>
/// R-196.1: Dialog for creating or editing cash/bank accounts.
/// </summary>
public partial class CashAccountEditDialog : Window
{
    public CashAccountEditDialog(CashAccountEditDialogViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    public CashAccountEditDialogViewModel ViewModel => (CashAccountEditDialogViewModel)DataContext;

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var vm = ViewModel;

        if (string.IsNullOrWhiteSpace(vm.Name))
        {
            MessageBox.Show("Hesap adi bos olamaz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (vm.Type == CashAccountType.Bank &&
            string.IsNullOrWhiteSpace(vm.Iban) &&
            string.IsNullOrWhiteSpace(vm.AccountNumber))
        {
            MessageBox.Show("Banka hesaplari icin IBAN veya Hesap No zorunludur.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

