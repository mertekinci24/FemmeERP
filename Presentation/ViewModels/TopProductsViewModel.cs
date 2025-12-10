using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryERP.Application.Reports;

namespace InventoryERP.Presentation.ViewModels;

public sealed class TopProductsViewModel : ViewModelBase
{
    private readonly IDashboardQueries _queries;
    private List<TopProductDto> _top = new();

    public TopProductsViewModel(IDashboardQueries queries) => _queries = queries;

    public IReadOnlyList<TopProductDto> TopProducts => _top;

    public async Task LoadAsync(DateTime? from = null, DateTime? to = null, int topN = 10)
    {
        var res = await _queries.GetTopProductsAsync(from, to, topN);
        _top = res.TopProducts;
        OnPropertyChanged(nameof(TopProducts));
    }
}



