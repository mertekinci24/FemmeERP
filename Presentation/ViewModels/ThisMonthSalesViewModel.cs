using System;
using System.Threading.Tasks;
using InventoryERP.Application.Reports;

namespace InventoryERP.Presentation.ViewModels;

public sealed class ThisMonthSalesViewModel : ViewModelBase
{
    private readonly IDashboardQueries _queries;
    private decimal _totalTry;
    private int _invoiceCount;

    public ThisMonthSalesViewModel(IDashboardQueries queries)
    {
        _queries = queries;
    }

    public decimal TotalTry { get => _totalTry; private set => SetProperty(ref _totalTry, value); }
    public int InvoiceCount { get => _invoiceCount; private set => SetProperty(ref _invoiceCount, value); }

    public string Display => $"{InvoiceCount} invoices â€” {TotalTry:C2}";

    public async Task LoadAsync(int? year = null, int? month = null)
    {
        var now = DateTime.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;
        var res = await _queries.GetThisMonthSalesAsync(y, m);
        TotalTry = res.TotalTry;
        InvoiceCount = res.InvoiceCount;
        OnPropertyChanged(nameof(Display));
    }
}



