using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using Xunit;

namespace Tests.Unit;

public class BarcodeAddLineTests
{
    private class CapturingCmd : Application.Documents.IDocumentCommandService
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

    private class StubProducts : IProductsReadService
    {
        private readonly ProductRowDto? _product;
        public StubProducts(ProductRowDto? product) => _product = product;
        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search) => Task.FromResult((IReadOnlyList<ProductRowDto>)new List<ProductRowDto>());
        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId) => Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto>());
        public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId) => Task.FromResult((IReadOnlyList<ProductLotDto>)new List<ProductLotDto>());
        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId) => Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult(_product);
    }

    [Fact]
    public async Task Barcode_Adds_New_Line_When_Not_Exists()
    {
        var dto = new DocumentDetailDto { Id = 1, Type = "SATIS_FATURASI", Lines = new List<DocumentLineDto>() };
        var prod = new ProductRowDto(5, "ABC123", "Test Product", "ADET", 1, true, 0m);
        var vm = new DocumentEditViewModel(dto, new CapturingCmd(), new StubProducts(prod), new StubDialogService());

        vm.BarcodeText = "ABC123";
        vm.AddBarcodeCmd.Execute(null);
        // wait a bit for async to complete
        for (int i = 0; i < 10 && vm.Lines.Count == 0; i++) await Task.Delay(20);

        Assert.Single(vm.Lines);
        Assert.Equal(5, vm.Lines[0].ItemId);
        Assert.Equal(1m, vm.Lines[0].Qty);
        Assert.Equal("ADET", vm.Lines[0].Uom);
        Assert.Equal(1, vm.Lines[0].VatRate);
    }

    [Fact]
    public async Task Barcode_Increments_Qty_When_Line_Exists()
    {
        var dto = new DocumentDetailDto { Id = 2, Type = "SATIS_FATURASI", Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 5, ItemName = "Test Product", Qty = 2m, Uom = "ADET", Coefficient = 1m, UnitPrice = 0m, VatRate = 1 } } };
        var prod = new ProductRowDto(5, "ABC123", "Test Product", "ADET", 1, true, 0m);
        var vm = new DocumentEditViewModel(dto, new CapturingCmd(), new StubProducts(prod), new StubDialogService());

        vm.BarcodeText = "ABC123";
        vm.AddBarcodeCmd.Execute(null);
        for (int i = 0; i < 10 && vm.Lines.First().Qty == 2m; i++) await Task.Delay(20);

        Assert.Equal(3m, vm.Lines.First().Qty);
    }
}
