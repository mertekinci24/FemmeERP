using System.Windows;

namespace InventoryERP.Presentation.Views
{
    /// <summary>
    /// R-172: Simple input dialog for getting text/number input from user
    /// </summary>
    public partial class InputDialog : Window
    {
        public string ResultText { get; private set; } = "";

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            DataContext = new InputDialogViewModel { Prompt = prompt, InputText = defaultValue };
            InputTextBox.SelectAll();
            InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = (InputDialogViewModel)DataContext;
            ResultText = vm.InputText;
            DialogResult = true;
        }

        private class InputDialogViewModel
        {
            public string Prompt { get; set; } = "";
            public string InputText { get; set; } = "";
        }
    }
}
