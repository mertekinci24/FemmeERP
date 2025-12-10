using System.Windows.Controls;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// R-045: Stock & Warehouse Module
    /// </summary>
    public partial class StockModuleView : UserControl
    {
        public StockModuleView(ViewModels.StockModuleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
