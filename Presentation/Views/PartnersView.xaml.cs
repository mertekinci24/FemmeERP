using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InventoryERP.Presentation.Helpers;
using InventoryERP.Presentation.ViewModels;

namespace InventoryERP.Presentation.Views
{
    public partial class PartnersView : UserControl
    {
        public PartnersView(PartnersViewModel viewModel)
        {
            if (TestEnvironmentDetector.IsRunningUnderTest)
            {
                Content = new TextBlock
                {
                    Text = "PartnersView (Test Mode)",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    Padding = new System.Windows.Thickness(16)
                };
            }
            else
            {
                InitializeComponent();
            }
            DataContext = viewModel;
        }

        private void PartnerRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                row.Focus();
                row.IsSelected = true;
            }
        }
    }
}
