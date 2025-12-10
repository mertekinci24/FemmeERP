using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using InventoryERP.Presentation.ViewModels;

namespace InventoryERP.Presentation.Views;

/// <summary>
/// R-086/R-194.2: Partner (Cari) List as UserControl for embedding in Shell
/// </summary>
public partial class PartnerListView : UserControl
{
    public PartnerListView(ViewModels.PartnerListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void PartnerRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row)
        {
            row.IsSelected = true;
            row.Focus();

            // R-156: Create ContextMenu programmatically to avoid XAML circular dependency issues
            if (DataContext is PartnerListViewModel vm)
            {
                vm.RebuildContextMenuItems();

                var contextMenu = new ContextMenu
                {
                    MinWidth = 220,
                    MinHeight = 90
                };

                // Set ItemsSource
                contextMenu.ItemsSource = vm.DynamicContextMenuItems;

                // Apply ItemContainerStyle
                var menuItemStyle = new Style(typeof(MenuItem));
                menuItemStyle.Setters.Add(new Setter(MenuItem.HeaderProperty, new Binding("Header")));
                menuItemStyle.Setters.Add(new Setter(MenuItem.CommandProperty, new Binding("Command")));
                menuItemStyle.Setters.Add(new Setter(MenuItem.CommandParameterProperty, new Binding("CommandParameter")));
                menuItemStyle.Setters.Add(new Setter(MenuItem.PaddingProperty, new Thickness(12, 8, 12, 8)));
                menuItemStyle.Setters.Add(new Setter(MenuItem.FontSizeProperty, 13.0));
                menuItemStyle.Setters.Add(new Setter(MenuItem.MinHeightProperty, 30.0));
                menuItemStyle.Setters.Add(new Setter(MenuItem.ForegroundProperty, Brushes.Black));
                menuItemStyle.Setters.Add(new Setter(MenuItem.BackgroundProperty, Brushes.White));

                contextMenu.ItemContainerStyle = menuItemStyle;

                // Assign to row and open
                row.ContextMenu = contextMenu;
                contextMenu.PlacementTarget = row;
                contextMenu.IsOpen = true;
            }
        }
    }
}
