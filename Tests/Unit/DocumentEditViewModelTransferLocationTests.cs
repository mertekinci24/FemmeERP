using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using Xunit;

namespace Tests.Unit;

public class DocumentEditViewModelTransferLocationTests
{
    private class CapturingCmd : Application.Documents.IDocumentCommandService
    {
        public DocumentDetailDto? LastDto;
        public Task<int> CreateDraftAsync(DocumentDetailDto dto) => Task.FromResult(0);
        public Task UpdateDraftAsync(int id, DocumentDetailDto dto) { LastDto = dto; return Task.CompletedTask; }
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
            => Task.FromResult((IReadOnlyList<ProductRowDto>)new List<ProductRowDto>());
        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto>());
        public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductLotDto>)new List<ProductLotDto>());
        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    [Fact]
    public async Task Transfer_requires_source_and_destination_locations()
    {
        var cmd = new CapturingCmd();
        var vm = new DocumentEditViewModel(
            new DocumentDetailDto { Id = 10, Type = "TRANSFER_FISI", Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 1, ItemName = "P", Qty = 1m, UnitPrice = 0m, Uom = "pcs", Coefficient = 1m, VatRate = 1 } } },
            cmd,
            new DummyProducts(),
            new Tests.Unit.TestHelpers.StubDialogService()
        );

        // Initially missing both locations should cause validation failure on save
        var ok1 = await vm.SaveAsync();
        Assert.False(ok1);
        Assert.True(vm.HasErrors);

        // Set only one should still fail
        vm.Lines[0].SourceLocationId = 1;
        var ok2 = await vm.SaveAsync();
        Assert.False(ok2);

        // Set both and distinct should pass and map into DTO
        vm.Lines[0].DestinationLocationId = 2;
        var ok3 = await vm.SaveAsync();
        Assert.True(ok3);
        Assert.NotNull(cmd.LastDto);
        var saved = cmd.LastDto!.Lines.First();
        Assert.Equal(1, saved.SourceLocationId);
        Assert.Equal(2, saved.DestinationLocationId);
    }
}
