using System.Windows.Controls;
using InventoryERP.Presentation.ViewModels;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// Warehouse Module - Depo Hareketleri ve Lokasyon YÃ¶netimi
    /// R-044.2: Part of Stock & Warehouse split
    /// </summary>
    public partial class WarehouseModuleView : UserControl
    {
        public WarehouseModuleView(WarehouseModuleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
