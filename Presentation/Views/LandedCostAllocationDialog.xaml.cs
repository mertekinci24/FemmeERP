using System.Windows;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Application.Documents;

namespace InventoryERP.Presentation.Views;

public partial class LandedCostAllocationDialog : Window
{
    private readonly LandedCostAllocationViewModel _vm;
    public LandedCostAllocationDialog(IDocumentQueries queries, ILandedCostService svc)
    {
        InitializeComponent();
        _vm = new LandedCostAllocationViewModel(queries, svc);
        DataContext = _vm;
        _vm.AllocationCompleted += ok => { if (ok) { DialogResult = true; Close(); } };
        Loaded += async (_, __) => await _vm.RefreshAsync();
    }
}


