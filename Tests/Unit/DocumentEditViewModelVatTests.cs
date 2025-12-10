using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using Xunit;

namespace Tests.Unit;

public class DocumentEditViewModelVatTests
{
    private class DummyCmd : Application.Documents.IDocumentCommandService
    {
        public Task<int> CreateDraftAsync(DocumentDetailDto dto) => Task.FromResult(0);
        public Task UpdateDraftAsync(int id, DocumentDetailDto dto) => Task.CompletedTask;
        public Task DeleteDraftAsync(int id) => Task.CompletedTask;
        public Task ApproveAsync(int id) => Task.CompletedTask;
        public Task CancelAsync(int id) => Task.CompletedTask;
        public Task<int> ConvertSalesOrderToDispatchAsync(int salesOrderId) => Task.FromResult(0);
        public Task<int> ConvertDispatchToInvoiceAsync(int dispatchId) => Task.FromResult(0);
        public Task SaveAndApproveAdjustmentAsync(int id, DocumentDetailDto dto) => Task.CompletedTask;
    }

    private class DummyProducts : IProductsReadService
    {
        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search)
            => Task.FromResult((IReadOnlyList<ProductRowDto>)new List<ProductRowDto>
            {
                new ProductRowDto(1, "SKU-1", "Prod-1", "PCS", 20, true, 0m)
            });
        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
            // Return empty to force fallback to BaseUom path in the ViewModel
            => Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto>());
        public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductLotDto>)new List<ProductLotDto>());
        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    [Fact]
    public async Task Selecting_Product_Auto_Fills_Vat_And_Defaults_Uom()
    {
        var dto = new DocumentDetailDto
        {
            Id = 1,
            Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 0, ItemName = string.Empty, Qty = 1m, UnitPrice = 0m, VatRate = 0, Uom = string.Empty, Coefficient = 1m } }
        };
        var vm = new DocumentEditViewModel(dto, new DummyCmd(), new DummyProducts(), new StubDialogService());

        // Act: select the product on the existing line
        vm.Lines[0].ItemId = 1;
        await Task.Delay(200); // allow async loaders to finish

        // Assert: VAT auto-filled and UOM defaulted to product base if none available
        Assert.Equal(20, vm.Lines[0].VatRate);
        Assert.Equal("PCS", vm.Lines[0].Uom);
    }
}
