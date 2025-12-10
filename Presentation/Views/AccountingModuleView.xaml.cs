using System.Windows.Controls;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// R-044.1: Accounting Module - Invoices and Partners (Cari Hesap)
    /// </summary>
    public partial class AccountingModuleView : UserControl
    {
        public AccountingModuleView(ViewModels.AccountingModuleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
