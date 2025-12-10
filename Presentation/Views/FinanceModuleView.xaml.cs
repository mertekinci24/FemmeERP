using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// R-044.1: Finance Module - Cash Flow (Kasa, Tahsilat, Ã–deme)
    /// </summary>
    public partial class FinanceModuleView : UserControl
    {
        public FinanceModuleView(ViewModels.FinanceModuleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
