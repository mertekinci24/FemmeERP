using System.Windows.Controls;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// R-060: Quotes List View
    /// </summary>
    public partial class QuotesView : UserControl
    {
        public QuotesView(ViewModels.QuotesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private async void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.QuotesViewModel vm)
            {
                await vm.EditSelectedAsync();
            }
        }
    }
}
