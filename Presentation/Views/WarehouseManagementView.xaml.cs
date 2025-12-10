using System.Windows.Controls;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// R-040: Warehouse Management View
    /// </summary>
    public partial class WarehouseManagementView : UserControl
    {
        public WarehouseManagementView(ViewModels.WarehouseManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
