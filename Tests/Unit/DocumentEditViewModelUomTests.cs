using System.Threading.Tasks;
using Xunit;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using System.Collections.Generic;

namespace Tests.Unit;

public class DocumentEditViewModelUomTests
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
        public Task<System.Collections.Generic.IReadOnlyList<ProductRowDto>> GetListAsync(string? search)
        {
            var list = new List<ProductRowDto>();
            return Task.FromResult((System.Collections.Generic.IReadOnlyList<ProductRowDto>)list);
        }

        public Task<System.Collections.Generic.IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
        {
            var list = new List<ProductUomDto> { new ProductUomDto("KOLI", 12m) };
            return Task.FromResult((System.Collections.Generic.IReadOnlyList<ProductUomDto>)list);
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
    public async Task Selecting_Product_Populates_Uoms_And_Selecting_Uom_Sets_Coefficient()
    {
        var dto = new DocumentDetailDto { Id = 1, Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 0, ItemName = "P", Qty = 1m, UnitPrice = 10m, VatRate = 18 } } };
        var vm = new DocumentEditViewModel(dto, new DummyCmd(), new DummyProducts(), new StubDialogService());

        // simulate user selecting a product in the line
        vm.Lines[0].ItemId = 1;

        // allow async load to complete
        await Task.Delay(200);

        Assert.True(vm.Lines[0].AvailableUoms.Count >= 1);

        // select KOLI
        vm.Lines[0].Uom = "KOLI";

        Assert.Equal(12m, vm.Lines[0].Coefficient);
    }
}
