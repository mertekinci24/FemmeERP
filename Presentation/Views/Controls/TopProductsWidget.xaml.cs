using System;
using System.Windows.Controls;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Application.Reports;

namespace InventoryERP.Presentation.Views.Controls;

public partial class TopProductsWidget : UserControl
{
    private readonly TopProductsViewModel _vm;
    public TopProductsWidget(IDashboardQueries queries)
    {
        InitializeComponent();
        _vm = new TopProductsViewModel(queries);
        DataContext = _vm;
        _ = _vm.LoadAsync();
        _vm.PropertyChanged += (s,e) => UpdateUi();
        UpdateUi();
    }

    private void UpdateUi()
    {
        if (TopList != null) TopList.ItemsSource = _vm.TopProducts;
    }
}


