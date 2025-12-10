using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InventoryERP.Presentation.Views;

/// <summary>
/// R-086: Partner Edit Dialog code-behind
/// </summary>
public partial class PartnerEditDialog : Window
{
    private readonly ViewModels.PartnerEditViewModel _viewModel;
    
    public PartnerEditDialog(ViewModels.PartnerEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        
        // R-089 FIX: Set title programmatically to avoid StaticResource parse-time crash
        Title = _viewModel.IsNewPartner ? "Yeni Cari OluÅŸtur" : "Cari DÃ¼zenle";
        Loaded += PartnerEditDialog_Loaded;
    }

    private async void PartnerEditDialog_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= PartnerEditDialog_Loaded;
        
        // R-148: Select Hareketler tab if requested
        if (_viewModel.SelectedTab == "Hareketler")
        {
            MainTabControl.SelectedIndex = 1; // 0=Kart, 1=Hareketler
        }
        
        await _viewModel.LoadLedgerAsync();
    }
    
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var success = await _viewModel.SaveAsync();
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// Converts IsNewPartner bool to window title
/// </summary>
public class BoolToPartnerTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isNew)
        {
            return isNew ? "Yeni Cari OluÅŸtur" : "Cari DÃ¼zenle";
        }
        return "Cari DÃ¼zenle";
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// R-089: Converts string to Visibility (Visible if not null/empty, Collapsed otherwise)
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
