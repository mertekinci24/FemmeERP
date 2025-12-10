using System.Threading.Tasks;
using Xunit;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using System.Collections.Generic;
using Tests.Unit.TestHelpers;

namespace Tests.Unit;

public class DocumentEditViewModelAddLineTests
{
    private sealed class DummyCmd : Application.Documents.IDocumentCommandService
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

    private sealed class DummyProducts : IProductsReadService
    {
        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search) => Task.FromResult((IReadOnlyList<ProductRowDto>)new List<ProductRowDto>());
        public Task<IReadOnlyList<Application.Products.ProductUomDto>> GetUomsAsync(int productId) => Task.FromResult((IReadOnlyList<Application.Products.ProductUomDto>)new List<Application.Products.ProductUomDto>());
        public Task<IReadOnlyList<Application.Products.ProductLotDto>> GetLotsForProductAsync(int productId) => Task.FromResult((IReadOnlyList<Application.Products.ProductLotDto>)new List<Application.Products.ProductLotDto>());
        public Task<IReadOnlyList<Application.Products.ProductVariantDto>> GetVariantsAsync(int productId) => Task.FromResult((IReadOnlyList<Application.Products.ProductVariantDto>)new List<Application.Products.ProductVariantDto>());
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    [Fact]
    public void AddLine_Adds_New_Line_And_Selects_It()
    {
        var dto = new DocumentDetailDto { Id = 20, Type = "QUOTE", Lines = new List<DocumentLineDto>() };
        var vm = new DocumentEditViewModel(dto, new DummyCmd(), new DummyProducts(), new StubDialogService());

        var initialCount = vm.Lines.Count;
        vm.AddLineCmd.Execute(null);
        Assert.True(vm.Lines.Count >= initialCount + 1);
        Assert.NotNull(vm.SelectedLine);
    }
}
