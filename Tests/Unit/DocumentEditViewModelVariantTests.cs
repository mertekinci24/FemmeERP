using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Tests.Unit.TestHelpers;
using Xunit;

namespace Tests.Unit;

public class DocumentEditViewModelVariantTests
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
            => Task.FromResult((IReadOnlyList<ProductRowDto>)new List<ProductRowDto> { new ProductRowDto(1, "SKU-1", "Prod", "PCS", 18, true, 0m) });
        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto> { new ProductUomDto("PCS", 1m) });
        public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductLotDto>)new List<ProductLotDto>());
        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId)
            => Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto> { new ProductVariantDto(100, "SKU-1-RED-L") });
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    [Fact]
    public async Task Production_requires_variant_selection_on_lines()
    {
        var cmd = new CapturingCmd();
        var vm = new DocumentEditViewModel(
            new DocumentDetailDto { Id = 20, Type = "URETIM_FISI", Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 1, ItemName = "P", Qty = 1m, UnitPrice = 0m, Uom = "PCS", Coefficient = 1m, VatRate = 1 } } },
            cmd,
            new DummyProducts(),
            new Tests.Unit.TestHelpers.StubDialogService()
        );

        // Without variant, validation should fail
        var fail = await vm.SaveAsync();
        Assert.False(fail);
        Assert.True(vm.HasErrors);

        // Set variant and pass
        vm.Lines[0].ProductVariantId = 100;
        var ok = await vm.SaveAsync();
        Assert.True(ok);
        Assert.NotNull(cmd.LastDto);
        Assert.Equal(100, cmd.LastDto!.Lines.First().ProductVariantId);
    }

    // R-081 regression guard: ensure DI can construct DocumentEditViewModel when only DTO is supplied
    [Fact]
    public void ActivatorUtilities_Resolves_DocumentEditViewModel_With_DTO_Only()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddScoped<Application.Documents.IDocumentCommandService, CapturingCmd>();
        services.AddScoped<IProductsReadService, DummyProducts>();
        services.AddSingleton<InventoryERP.Presentation.Abstractions.IDialogService, Tests.Unit.TestHelpers.StubDialogService>();
        services.Configure<InventoryERP.Presentation.Configuration.UiOptions>(o => o.DebounceMs = 10);
        using var sp = services.BuildServiceProvider();
        var dto = new DocumentDetailDto { Id = 99, Type = "QUOTE", Currency = "TRY", Date = System.DateTime.Today, Lines = new List<DocumentLineDto>() };
        var vm = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<DocumentEditViewModel>(sp, dto);
        Assert.NotNull(vm);
        Assert.Equal("QUOTE", vm.Dto.Type);
    }
}
