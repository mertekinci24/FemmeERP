using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Presentation.Commands;

namespace InventoryERP.Presentation.ViewModels;

public sealed class LandedCostAllocationViewModel : ViewModelBase
{
    private readonly IDocumentQueries _queries;
    private readonly ILandedCostService _svc;

    public ObservableCollection<InventoryERP.Application.Documents.DocumentRowDto> PurchaseInvoices { get; } = new();
    public ObservableCollection<SelectableDocRow> GoodsReceipts { get; } = new();

    private int? _selectedInvoiceId;
    public int? SelectedInvoiceId { get => _selectedInvoiceId; set { if (SetProperty(ref _selectedInvoiceId, value)) { AllocateCmd.RaiseCanExecuteChanged(); } } }

    private string? _error;
    public string? Error { get => _error; set => SetProperty(ref _error, value); }

    public AsyncRelayCommand RefreshCmd { get; }
    public AsyncRelayCommand AllocateCmd { get; }
    public RelayCommand CancelCmd { get; }

    public event Action<bool>? AllocationCompleted;

    public LandedCostAllocationViewModel(IDocumentQueries queries, ILandedCostService svc)
    {
        _queries = queries;
        _svc = svc;
    RefreshCmd = new AsyncRelayCommand(RefreshAsync);
    AllocateCmd = new AsyncRelayCommand(AllocateAsync, CanAllocate);
        CancelCmd = new RelayCommand(_ => AllocationCompleted?.Invoke(false));
    }

    public async Task RefreshAsync()
    {
        Error = null;
        PurchaseInvoices.Clear();
        GoodsReceipts.Clear();

        // load purchase invoices (SATINALMA_FATURASI aka PURCHASE_INVOICE)
        var invFilter = new DocumentListFilter { Type = "PURCHASE_INVOICE", SortBy = "Date", SortDir = "DESC" };
        var invs = await _queries.ListAsync(invFilter, 1, 100);
        foreach (var r in invs.Items) PurchaseInvoices.Add(r);

        // load goods receipts (GELEN_IRSALIYE)
        var grFilter = new DocumentListFilter { Type = "GELEN_IRSALIYE", SortBy = "Date", SortDir = "DESC" };
        var grs = await _queries.ListAsync(grFilter, 1, 200);
        foreach (var r in grs.Items) GoodsReceipts.Add(new SelectableDocRow(r));
    }

    private bool CanAllocate()
    {
        return SelectedInvoiceId.HasValue && GoodsReceipts.Any(x => x.IsSelected);
    }

    public async Task AllocateAsync()
    {
        Error = null;
        if (!CanAllocate()) { Error = "Fatura ve en az bir mal kabul seÃ§iniz."; return; }

        var ids = GoodsReceipts.Where(x => x.IsSelected).Select(x => x.Row.Id).ToList();
        try
        {
            await _svc.ApplyAsync(SelectedInvoiceId!.Value, ids);
            AllocationCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    public sealed class SelectableDocRow : ViewModelBase
    {
        public InventoryERP.Application.Documents.DocumentRowDto Row { get; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        public SelectableDocRow(InventoryERP.Application.Documents.DocumentRowDto row) { Row = row; }
    }
}




