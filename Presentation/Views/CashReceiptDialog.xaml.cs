using System.Windows;
using InventoryERP.Presentation.ViewModels.Cash;

namespace InventoryERP.Presentation.Views;

/// <summary>
/// R-131: Cash Receipt Dialog (Tahsilat FiÅŸi)
/// </summary>
public partial class CashReceiptDialog : Window
{
    public CashReceiptDialog(CashReceiptDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (s, e) => 
        {
            DialogResult = viewModel.DialogResult;
            Close();
        };
    }
}
