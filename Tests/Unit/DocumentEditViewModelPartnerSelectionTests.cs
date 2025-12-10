using System.Threading.Tasks;
using Xunit;
using InventoryERP.Presentation.ViewModels;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Application.Partners;
using InventoryERP.Domain.Enums; // R-092: Required for PartnerType enum
using System.Collections.Generic;
using System.Linq; // R-092: Required for LINQ operations
using Tests.Unit.TestHelpers;

namespace Tests.Unit;

public class DocumentEditViewModelPartnerSelectionTests
{
    private sealed class StubCmd : Application.Documents.IDocumentCommandService
    {
        public bool Updated;
        public Task<int> CreateDraftAsync(DocumentDetailDto dto) => Task.FromResult(0);
        public Task UpdateDraftAsync(int id, DocumentDetailDto dto) { Updated = true; return Task.CompletedTask; }
        public Task DeleteDraftAsync(int id) => Task.CompletedTask;
        public Task ApproveAsync(int id) => Task.CompletedTask;
        public Task CancelAsync(int id) => Task.CompletedTask;
        public Task<int> ConvertSalesOrderToDispatchAsync(int salesOrderId) => Task.FromResult(0);
        public Task<int> ConvertDispatchToInvoiceAsync(int dispatchId) => Task.FromResult(0);
        public Task SaveAndApproveAdjustmentAsync(int id, DocumentDetailDto dto) => Task.CompletedTask;
    }

    private sealed class StubProducts : IProductsReadService
    {
        private readonly IReadOnlyList<ProductRowDto> _rows;
        public StubProducts(IReadOnlyList<ProductRowDto> rows) { _rows = rows; }
        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search) => Task.FromResult(_rows);
        public Task<IReadOnlyList<Application.Products.ProductUomDto>> GetUomsAsync(int productId) => Task.FromResult((IReadOnlyList<Application.Products.ProductUomDto>)new List<Application.Products.ProductUomDto>());
        public Task<IReadOnlyList<Application.Products.ProductLotDto>> GetLotsForProductAsync(int productId) => Task.FromResult((IReadOnlyList<Application.Products.ProductLotDto>)new List<Application.Products.ProductLotDto>());
        public Task<IReadOnlyList<Application.Products.ProductVariantDto>> GetVariantsAsync(int productId) => Task.FromResult((IReadOnlyList<Application.Products.ProductVariantDto>)new List<Application.Products.ProductVariantDto>());
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    // R-092: Updated to implement IPartnerService (R-086) instead of obsolete IPartnerReadService
    private sealed class StubPartners : IPartnerService
    {
        private readonly List<PartnerCrudListDto> _rows;
        
        public StubPartners(List<PartnerCrudListDto> rows) { _rows = rows; }
        
        public Task<List<PartnerCrudListDto>> GetListAsync(PartnerType? filterByType = null, System.Threading.CancellationToken ct = default) 
            => Task.FromResult(_rows);
        
        public Task<PartnerCrudDetailDto?> GetByIdAsync(int id, System.Threading.CancellationToken ct = default) 
            => Task.FromResult<PartnerCrudDetailDto?>(null);
        
        public Task<int> SaveAsync(PartnerCrudDetailDto dto, System.Threading.CancellationToken ct = default) 
            => Task.FromResult(0);
        
        public Task DeleteAsync(int id, System.Threading.CancellationToken ct = default) 
            => Task.CompletedTask;
    }

    [Fact]
    public async Task SelectingPartner_PopulatesTitle_And_Save_Passes()
    {
        var dto = new DocumentDetailDto { Id = 10, Type = "SALES_INVOICE", Lines = new List<DocumentLineDto> { new() { ItemId = 1, ItemName = "Prod", Qty = 1m, UnitPrice = 10m, VatRate = 18 } } };
        var cmd = new StubCmd();
        var products = new StubProducts(new List<ProductRowDto>());
        // R-092: Use PartnerCrudListDto (R-086) instead of obsolete PartnerRowDto
        var partners = new StubPartners(new List<PartnerCrudListDto> 
        { 
            new(Id: 5, Name: "Test Cari", PartnerType: PartnerType.Customer.ToString(), 
                TaxId: "", NationalId: null, IsActive: true) 
        });
        var vm = new DocumentEditViewModel(dto, cmd, products, new StubDialogService(), new Tests.Unit.TestHelpers.StubReportService(), new Tests.Unit.TestHelpers.StubFileDialogService(), partners);

        // Wait for async partner load
        await Task.Delay(200);
        
        // simulate partner selection
        vm.PartnerId = 5;
        Assert.Equal(5, vm.PartnerId);
        Assert.Equal("Test Cari", vm.PartnerTitle); // R-092: Should use 'Name' from PartnerCrudListDto

        var ok = await vm.SaveAsync();
        Assert.True(ok);
        Assert.True(cmd.Updated);
    }

    [Fact]
    public async Task MissingPartner_ShouldFail_For_RequiredDocuments()
    {
        var dto = new DocumentDetailDto { Id = 11, Type = "SALES_INVOICE", Lines = new List<DocumentLineDto> { new() { ItemId = 1, ItemName = "P", Qty = 1m, UnitPrice = 5m, VatRate = 18 } } };
        var vm = new DocumentEditViewModel(dto, new StubCmd(), new StubProducts(new List<ProductRowDto>()), new StubDialogService(), new Tests.Unit.TestHelpers.StubReportService(), new Tests.Unit.TestHelpers.StubFileDialogService(), new StubPartners(new List<PartnerCrudListDto>()));
        // leave PartnerId null
        var ok = await vm.SaveAsync();
        Assert.False(ok);
    }

    [Fact]
    public async Task Adjustment_Document_DoesNotRequirePartner()
    {
        var dto = new DocumentDetailDto { Id = 12, Type = "ADJUSTMENT_OUT", Lines = new List<DocumentLineDto> { new() { ItemId = 1, ItemName = "P", Qty = 2m, UnitPrice = 3m, VatRate = 18 } } };
        var ok = await new DocumentEditViewModel(dto, new StubCmd(), new StubProducts(new List<ProductRowDto>()), new StubDialogService(), new Tests.Unit.TestHelpers.StubReportService(), new Tests.Unit.TestHelpers.StubFileDialogService(), new StubPartners(new List<PartnerCrudListDto>())).SaveAsync();
        Assert.True(ok);
    }
}
