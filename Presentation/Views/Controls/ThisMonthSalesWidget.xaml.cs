using System.Windows.Controls;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Application.Reports;

namespace InventoryERP.Presentation.Views.Controls;

public partial class ThisMonthSalesWidget : UserControl
{
    private readonly ThisMonthSalesViewModel _vm;
    public ThisMonthSalesWidget(IDashboardQueries queries)
    {
        InitializeComponent();
        _vm = new ThisMonthSalesViewModel(queries);
        DataContext = _vm;
        // fire-and-forget loading (UI will update via data binding if used)
        _ = _vm.LoadAsync();
        _vm.PropertyChanged += (s,e) => UpdateTexts();
        UpdateTexts();
    }

    private void UpdateTexts()
    {
        if (CountText != null) CountText.Text = $"{_vm.InvoiceCount} invoices";
        if (TotalText != null) TotalText.Text = _vm.TotalTry.ToString("C2");
    }
}


