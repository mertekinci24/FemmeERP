using System.Threading.Tasks;
using Xunit;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Application.Partners; // R-092: Required for IPartnerService
using InventoryERP.Domain.Enums; // R-092: Required for PartnerType enum
using System.Collections.Generic;
using System.Linq;

namespace Tests.Unit;

public class DocumentEditViewModelTests
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

    // R-092: Mock IPartnerService for testing Partner loading
    private class DummyPartnerService : IPartnerService
    {
        private readonly List<PartnerCrudListDto> _partners = new();

        public DummyPartnerService()
        {
            // Seed with test data: 2 Customers, 1 Supplier
            _partners.Add(new PartnerCrudListDto(
                Id: 1, 
                Name: "Customer A", 
                PartnerType: PartnerType.Customer.ToString(),
                TaxId: "1111111111",
                NationalId: null,
                IsActive: true
            ));
            _partners.Add(new PartnerCrudListDto(
                Id: 2, 
                Name: "Customer B", 
                PartnerType: PartnerType.Customer.ToString(),
                TaxId: "2222222222",
                NationalId: null,
                IsActive: true
            ));
            _partners.Add(new PartnerCrudListDto(
                Id: 3, 
                Name: "Supplier X", 
                PartnerType: PartnerType.Supplier.ToString(),
                TaxId: "3333333333",
                NationalId: null,
                IsActive: true
            ));
        }

        public Task<List<PartnerCrudListDto>> GetListAsync(PartnerType? filterByType = null, System.Threading.CancellationToken ct = default)
        {
            // R-093 FIX: Apply filtering in mock to match real implementation
            if (filterByType.HasValue)
            {
                var filtered = _partners.Where(p => p.PartnerType == filterByType.Value.ToString()).ToList();
                return Task.FromResult(filtered);
            }
            return Task.FromResult(_partners);
        }

        public Task<PartnerCrudDetailDto?> GetByIdAsync(int id, System.Threading.CancellationToken ct = default) 
            => Task.FromResult<PartnerCrudDetailDto?>(null);
        
        public Task<int> SaveAsync(PartnerCrudDetailDto dto, System.Threading.CancellationToken ct = default) 
            => Task.FromResult(0);
        
        public Task DeleteAsync(int id, System.Threading.CancellationToken ct = default) 
            => Task.CompletedTask;
    }

    [Fact]
    public void Totals_Update_On_Line_Change()
    {
        var dto = new DocumentDetailDto { Id = 1, Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 1, ItemName = "P", Qty = 2m, UnitPrice = 10m, VatRate = 18 } } };
        var vm = new DocumentEditViewModel(dto, new DummyCmd(), new DummyProducts(), new StubDialogService(), new Tests.Unit.TestHelpers.StubReportService(), new Tests.Unit.TestHelpers.StubFileDialogService());

        Assert.Equal(20m, vm.Subtotal);
        Assert.Equal(3.6m, vm.VatTotal);
        Assert.Equal(23.6m, vm.GrandTotal);

        // change qty
        vm.Lines[0].Qty = 3m;
        Assert.Equal(30m, vm.Subtotal);
        Assert.Equal(5.4m, vm.VatTotal);
        Assert.Equal(35.4m, vm.GrandTotal);
    }

    [Fact]
    public async Task SaveAsync_ReturnsFalse_When_Invalid_Lines()
    {
        var dto = new DocumentDetailDto { Id = 2, Lines = new List<DocumentLineDto> { new DocumentLineDto { ItemId = 0, ItemName = "", Qty = 0m, UnitPrice = 0m, VatRate = 0 } } };
        var vm = new DocumentEditViewModel(dto, new DummyCmd(), new DummyProducts(), new StubDialogService());

        // line is invalid -> HasErrors on child
        Assert.True(vm.Lines[0].HasErrors);
        // parent should reflect errors
        Assert.True(vm.HasErrors);

        var ok = await vm.SaveAsync();
        Assert.False(ok);
    }

    [Fact]
    public void ProductsView_Filters_On_SearchText()
    {
        var dto = new DocumentDetailDto { Id = 3 };
        var vm = new DocumentEditViewModel(dto, new DummyCmd(), new DummyProducts(), new StubDialogService());

    vm.Products.Add(new ProductRowDto(1, "SKU1", "Apple", "Pcs", 18, true, 0m));
    vm.Products.Add(new ProductRowDto(2, "SKU2", "Banana", "Pcs", 8, true, 0m));
    vm.Products.Add(new ProductRowDto(3, "SKU3", "Apricot", "Pcs", 18, true, 0m));

        vm.SearchText = "Ap";
        var visible = vm.ProductsView.Cast<ProductRowDto>().ToList();
        Assert.Equal(2, visible.Count);
    }

    /// <summary>
    /// R-092: Test that DocumentEditViewModel loads partners from IPartnerService
    /// and filters to Customer-only for Quote documents
    /// </summary>
    [Fact]
    public async Task Partners_Are_Loaded_And_Filtered_To_Customers_Only()
    {
        // Arrange: Create a QUOTE document (requires partner selection)
        var dto = new DocumentDetailDto 
        { 
            Id = 100, 
            Type = "QUOTE", // Quote requires partner
            Lines = new List<DocumentLineDto>()
        };
        
        var partnerService = new DummyPartnerService();
        
        // Act: Create ViewModel with IPartnerService
        var vm = new DocumentEditViewModel(
            dto, 
            new DummyCmd(), 
            new DummyProducts(), 
            new StubDialogService(),
            new Tests.Unit.TestHelpers.StubReportService(),
            new Tests.Unit.TestHelpers.StubFileDialogService(),
            partnerService
        );
        
        // Wait for async loading to complete
        await Task.Delay(500); // LoadPartnersAsync is fire-and-forget in constructor
        
        // Assert: Partners collection should be populated with Customer-only partners
        Assert.NotEmpty(vm.Partners);
        Assert.Equal(2, vm.Partners.Count); // Should have only 2 Customers (not the Supplier)
        Assert.All(vm.Partners, p => Assert.Equal(PartnerType.Customer.ToString(), p.PartnerType));
        Assert.Contains(vm.Partners, p => p.Name == "Customer A");
        Assert.Contains(vm.Partners, p => p.Name == "Customer B");
        Assert.DoesNotContain(vm.Partners, p => p.Name == "Supplier X");
    }
}
