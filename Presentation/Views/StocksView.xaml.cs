using System.Windows.Controls;
using System.Windows.Input;
using InventoryERP.Presentation.ViewModels;

namespace InventoryERP.Presentation.Views
{
    public partial class StocksView : UserControl
    {
        public StocksView(StocksViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // R-039: Clear selection when clicking empty space in DataGrid
        private void DataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid dataGrid) return;
            if (DataContext is not StocksViewModel viewModel) return;

            // Check if click is on empty space (not on a row)
            var hitTest = dataGrid.InputHitTest(e.GetPosition(dataGrid));
            if (hitTest is not System.Windows.FrameworkElement element) return;

            // If clicked element is not part of a DataGridRow, clear selection
            if (element.FindVisualParent<DataGridRow>() == null)
            {
                viewModel.Selected = null;
            }
        }
    }

    // Helper extension for finding visual parent
    internal static class VisualTreeHelperExtensions
    {
        public static T? FindVisualParent<T>(this System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            var parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }
    }
}
