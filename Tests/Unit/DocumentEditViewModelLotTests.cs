using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using Xunit;

namespace Tests.Unit;

public class DocumentEditViewModelLotTests
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
        private readonly IReadOnlyList<ProductLotDto> _lots;
        public DummyProducts(IReadOnlyList<ProductLotDto> lots) => _lots = lots;
        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search)
            => Task.FromResult((IReadOnlyList<ProductRowDto>)new List<ProductRowDto>());
        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto>());
        public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId)
            => Task.FromResult(_lots);
        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    [Fact]
    public async Task Selecting_Product_Loads_Available_Lots()
    {
        var lots = new List<ProductLotDto> { new ProductLotDto(10, "L-001", new DateTime(2026,1,1)), new ProductLotDto(11, "L-002", null) };
        var dto = new DocumentDetailDto { Id = 1, Type = "GELEN_IRSALIYE", Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 0, ItemName = "P", Qty = 1m, UnitPrice = 10m, VatRate = 18 } } };
        var vm = new DocumentEditViewModel(dto, new CapturingCmd(), new DummyProducts(lots), new StubDialogService());

        vm.Lines[0].ItemId = 123;
        await Task.Delay(50);

        Assert.Equal(2, vm.Lines[0].AvailableLots.Count);
    }

    [Fact]
    public async Task Save_Maps_SelectedLotId_To_Dto()
    {
        var cmd = new CapturingCmd();
        var vm = new DocumentEditViewModel(
            new DocumentDetailDto { Id = 5, Type = "GELEN_IRSALIYE", Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 1, ItemName = "P", Qty = 1m, UnitPrice = 1m, VatRate = 1 } } },
            cmd,
            new DummyProducts(new List<ProductLotDto> { new ProductLotDto(99, "LOT-X", null) }),
            new StubDialogService()
        );

        vm.Lines[0].SelectedLotId = 99;
        var ok = await vm.SaveAsync();
        Assert.True(ok);
        Assert.NotNull(cmd.LastDto);
        Assert.Equal(99, cmd.LastDto!.Lines.First().LotId);
        Assert.Null(cmd.LastDto!.Lines.First().LotNumber);
    }

    [Fact]
    public async Task Save_Maps_NewLot_To_Dto()
    {
        var cmd = new CapturingCmd();
        var vm = new DocumentEditViewModel(
            new DocumentDetailDto { Id = 6, Type = "GELEN_IRSALIYE", Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 1, ItemName = "P", Qty = 1m, UnitPrice = 1m, VatRate = 1 } } },
            cmd,
            new DummyProducts(new List<ProductLotDto>()),
            new StubDialogService()
        );

        vm.Lines[0].NewLotNumber = "NEW-001";
        vm.Lines[0].NewExpiryDate = new DateTime(2027,12,31);
        var ok = await vm.SaveAsync();
        Assert.True(ok);
        Assert.NotNull(cmd.LastDto);
        var saved = cmd.LastDto!.Lines.First();
        Assert.Null(saved.LotId);
        Assert.Equal("NEW-001", saved.LotNumber);
        Assert.Equal(new DateTime(2027,12,31), saved.ExpiryDate);
    }
}
