using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// R-060: Sales & Marketing Module - Quotes and Sales Orders
    /// </summary>
    public partial class SalesModuleView : UserControl
    {
        public SalesModuleView(ViewModels.SalesModuleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
