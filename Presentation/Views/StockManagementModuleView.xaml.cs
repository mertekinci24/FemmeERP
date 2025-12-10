using System.Windows.Controls;
using InventoryERP.Presentation.ViewModels;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// Stock Management Module - Stok KartlarÄ± ve Stok Hareketleri
    /// R-044.2: Part of Stock & Warehouse split
    /// </summary>
    public partial class StockManagementModuleView : UserControl
    {
        public StockManagementModuleView(StockManagementModuleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
