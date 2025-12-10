using System.Windows.Controls;

namespace InventoryERP.Presentation.Views;

/// <summary>
/// R-131: Cash Account List View
/// </summary>
public partial class CashAccountListView : UserControl
{
    public CashAccountListView(ViewModels.Cash.CashAccountListViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
