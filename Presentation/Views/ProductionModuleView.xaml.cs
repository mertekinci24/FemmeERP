using System.Windows.Controls;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// R-045: Production (MRP) Module
    /// </summary>
    public partial class ProductionModuleView : UserControl
    {
        public ProductionModuleView(ViewModels.ProductionModuleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
