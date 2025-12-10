using System.Windows.Controls;
using InventoryERP.Presentation.ViewModels;

namespace InventoryERP.Presentation.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView(ReportsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
