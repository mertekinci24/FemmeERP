using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryERP.Application.Reports;

namespace InventoryERP.Presentation.ViewModels;

public sealed class OverdueReceivablesViewModel : ViewModelBase
{
    private readonly IDashboardQueries _queries;
    private decimal _totalOverdue;
    private int _overduePartnerCount;
    private List<OverduePartnerDto> _topPartners = new();

    public OverdueReceivablesViewModel(IDashboardQueries queries) => _queries = queries;

    public decimal TotalOverdue { get => _totalOverdue; private set => SetProperty(ref _totalOverdue, value); }
    public int OverduePartnerCount { get => _overduePartnerCount; private set => SetProperty(ref _overduePartnerCount, value); }
    public IReadOnlyList<OverduePartnerDto> TopPartners => _topPartners;

    public async Task LoadAsync(DateTime? asOf = null)
    {
        var res = await _queries.GetOverdueReceivablesAsync(asOf);
        TotalOverdue = res.TotalOverdueTry;
        OverduePartnerCount = res.OverduePartnerCount;
        _topPartners = res.TopPartners;
        OnPropertyChanged(nameof(TopPartners));
    }
}



