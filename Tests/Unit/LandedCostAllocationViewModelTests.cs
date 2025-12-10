using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using DTOs = InventoryERP.Application.Documents.DTOs;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Presentation.ViewModels;
using Xunit;

namespace Tests.Unit;

public class LandedCostAllocationViewModelTests
{
    private sealed class FakeQueries : IDocumentQueries
    {
        public Task<DTOs.DocumentDetailDto?> GetAsync(int id) => Task.FromResult<DTOs.DocumentDetailDto?>(null);

        public Task<PagedResult<DocumentRowDto>> ListAsync(DTOs.DocumentListFilter filter, int page, int pageSize)
        {
            if (string.Equals(filter.Type, "PURCHASE_INVOICE", System.StringComparison.OrdinalIgnoreCase))
            {
                var items = new List<DocumentRowDto> { new DocumentRowDto(100, "INV-100", "PURCHASE_INVOICE", System.DateTime.Today, "SUPPLIER", "APPROVED", 0m, 0m, 50m) };
                return Task.FromResult(new PagedResult<DocumentRowDto>(items, items.Count));
            }
            else if (string.Equals(filter.Type, "GELEN_IRSALIYE", System.StringComparison.OrdinalIgnoreCase))
            {
                var items = new List<DocumentRowDto>
                {
                    new DocumentRowDto(1, "GR-1", "GELEN_IRSALIYE", System.DateTime.Today, "SUPPLIER", "POSTED", 100m, 0m, 100m),
                    new DocumentRowDto(2, "GR-2", "GELEN_IRSALIYE", System.DateTime.Today, "SUPPLIER", "POSTED", 200m, 0m, 200m)
                };
                return Task.FromResult(new PagedResult<DocumentRowDto>(items, items.Count));
            }
            return Task.FromResult(new PagedResult<DocumentRowDto>(new List<DocumentRowDto>(), 0));
        }
    }

    private sealed class FakeSvc : ILandedCostService
    {
        public int? LastInvoiceId { get; private set; }
        public IReadOnlyList<int>? LastTargets { get; private set; }
        public Task ApplyAsync(int purchaseInvoiceId, IReadOnlyList<int> goodsReceiptDocumentIds, System.Threading.CancellationToken ct = default)
        {
            LastInvoiceId = purchaseInvoiceId;
            LastTargets = goodsReceiptDocumentIds;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Refresh_Loads_Invoices_And_GoodsReceipts()
    {
        var vm = new LandedCostAllocationViewModel(new FakeQueries(), new FakeSvc());
        await vm.RefreshAsync();
        Assert.NotEmpty(vm.PurchaseInvoices);
        Assert.NotEmpty(vm.GoodsReceipts);
    }

    [Fact]
    public async Task Allocate_Calls_Service_With_Selected_Documents()
    {
        var svc = new FakeSvc();
        var vm = new LandedCostAllocationViewModel(new FakeQueries(), svc);
        await vm.RefreshAsync();

        vm.SelectedInvoiceId = vm.PurchaseInvoices.First().Id;
        vm.GoodsReceipts[0].IsSelected = true;

        await vm.AllocateAsync();

        Assert.Equal(vm.SelectedInvoiceId, svc.LastInvoiceId);
        Assert.NotNull(svc.LastTargets);
        Assert.Contains(vm.GoodsReceipts[0].Row.Id, svc.LastTargets!);
    }
}
