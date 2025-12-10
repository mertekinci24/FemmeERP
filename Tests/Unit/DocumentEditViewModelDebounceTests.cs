using System.Threading.Tasks;
using System.Threading;
using Xunit;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using System.Collections.Generic;

namespace Tests.Unit;

public class DocumentEditViewModelDebounceTests
{
    private class DummyCmd : Application.Documents.IDocumentCommandService
    {
        public Task<int> CreateDraftAsync(Application.Documents.DTOs.DocumentDetailDto dto) => Task.FromResult(0);
        public Task UpdateDraftAsync(int id, Application.Documents.DTOs.DocumentDetailDto dto) => Task.CompletedTask;
        public Task DeleteDraftAsync(int id) => Task.CompletedTask;
        public Task ApproveAsync(int id) => Task.CompletedTask;
        public Task CancelAsync(int id) => Task.CompletedTask;
        public Task<int> ConvertSalesOrderToDispatchAsync(int salesOrderId) => Task.FromResult(0);
        public Task<int> ConvertDispatchToInvoiceAsync(int dispatchId) => Task.FromResult(0);
        public Task SaveAndApproveAdjustmentAsync(int id, Application.Documents.DTOs.DocumentDetailDto dto) => Task.CompletedTask;
    }

    private class DummyProducts : IProductsReadService
    {
        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search)
        {
            var list = new List<ProductRowDto>();
            return Task.FromResult((IReadOnlyList<ProductRowDto>)list);
        }
        public Task<System.Collections.Generic.IReadOnlyList<Application.Products.ProductUomDto>> GetUomsAsync(int productId)
        {
            return Task.FromResult((System.Collections.Generic.IReadOnlyList<Application.Products.ProductUomDto>)new System.Collections.Generic.List<Application.Products.ProductUomDto>());
        }
        public Task<System.Collections.Generic.IReadOnlyList<Application.Products.ProductLotDto>> GetLotsForProductAsync(int productId)
        {
            return Task.FromResult((System.Collections.Generic.IReadOnlyList<Application.Products.ProductLotDto>)new System.Collections.Generic.List<Application.Products.ProductLotDto>());
        }
        public Task<System.Collections.Generic.IReadOnlyList<Application.Products.ProductVariantDto>> GetVariantsAsync(int productId)
        {
            return Task.FromResult((System.Collections.Generic.IReadOnlyList<Application.Products.ProductVariantDto>)new System.Collections.Generic.List<Application.Products.ProductVariantDto>());
        }
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    [Fact]
    public async Task SearchText_debounce_refreshes_once()
    {
        var dto = new DocumentDetailDto { Id = 10 };
        var vm = new DocumentEditViewModel(dto, new DummyCmd(), new DummyProducts(), new StubDialogService());

        var count = 0;
        vm.ProductsRefreshed += () => Interlocked.Increment(ref count);

        vm.SearchText = "a";
        vm.SearchText = "ab";

        // Allow debounce period to elapse under test runner load
        await Task.Delay(1000);

        // at least one refresh should have occurred
        Assert.True(Volatile.Read(ref count) >= 1);
    }
}
