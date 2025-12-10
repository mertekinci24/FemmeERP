using System.Windows.Controls;
using System.Windows.Input;
using InventoryERP.Presentation.ViewModels;

namespace InventoryERP.Presentation.Views
{
    public partial class DocumentsView : UserControl
    {
        public DocumentsView(DocumentsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not DocumentsViewModel vm) return;
            // invoke edit command if available
            if (vm.EditCommand?.CanExecute(null) == true)
                vm.EditCommand.Execute(null);
        }
    }
}
