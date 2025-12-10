using System;
using System.Windows.Controls;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Application.Reports;

namespace InventoryERP.Presentation.Views.Controls;

public partial class OverdueReceivablesWidget : UserControl
{
    private readonly OverdueReceivablesViewModel _vm;
    public OverdueReceivablesWidget(IDashboardQueries queries)
    {
        InitializeComponent();
        _vm = new OverdueReceivablesViewModel(queries);
        DataContext = _vm;
        _ = _vm.LoadAsync();
        _vm.PropertyChanged += (s,e) => UpdateUi();
        UpdateUi();
    }

    private void UpdateUi()
    {
        if (SummaryText != null) SummaryText.Text = $"{_vm.TotalOverdue:C2} â€” {_vm.OverduePartnerCount} partners";
        if (TopList != null) TopList.ItemsSource = _vm.TopPartners;
    }
}


