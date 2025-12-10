using System.Windows;
using InventoryERP.Presentation.ViewModels.Cash;

namespace InventoryERP.Presentation.Views;

/// <summary>
/// R-131: Cash Payment Dialog (Ã–deme FiÅŸi)
/// </summary>
public partial class CashPaymentDialog : Window
{
    public CashPaymentDialog(CashPaymentDialogViewModel viewModel)
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
