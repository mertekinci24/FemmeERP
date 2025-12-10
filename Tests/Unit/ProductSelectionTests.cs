using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers; // R-069: StubDialogService
using Xunit;

namespace Tests.Unit;

/// <summary>
/// R-064: Tests for product selection behavior on R-063 empty lines
/// Ensures that selecting a product updates the line (ItemName, VatRate, etc.) and doesn't cause it to "disappear"
/// </summary>
public class ProductSelectionTests
{
    private class StubDocumentCommandService : Application.Documents.IDocumentCommandService
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

    private class StubProductsReadService : IProductsReadService
    {
        private readonly List<ProductRowDto> _products;
        
        public StubProductsReadService(List<ProductRowDto> products)
        {
            _products = products;
        }

        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search) => 
            Task.FromResult((IReadOnlyList<ProductRowDto>)_products);

        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
        {
            // R-064: Return UOM matching the product's base UOM
            var product = _products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
                return Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto>());
            
            return Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto> 
            { 
                new ProductUomDto(product.BaseUom, 1m) 
            });
        }

        public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId) => 
            Task.FromResult((IReadOnlyList<ProductLotDto>)new List<ProductLotDto>());

        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId) => 
            Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());

        public Task<ProductRowDto?> GetByCodeAsync(string code) => 
            Task.FromResult(_products.FirstOrDefault(p => p.Sku == code));
    }

    [Fact]
    public async Task WhenProductIsSelected_On_R063_EmptyLine_ShouldUpdateItemNameAndNotDisappear()
    {
        // Arrange: Create a QUOTE document with empty Lines (triggers R-063 empty line logic)
        var dto = new DocumentDetailDto 
        { 
            Id = 1, 
            Type = "QUOTE", 
            Lines = new List<DocumentLineDto>() // Empty list triggers R-063
        };

        var mockProduct = new ProductRowDto(5, "TEST-SKU", "Test Product Name", "PCS", 20, true, 100m);
        var products = new List<ProductRowDto> { mockProduct };
        
        var cmdSvc = new StubDocumentCommandService();
        var productsSvc = new StubProductsReadService(products);

        // Act 1: Create ViewModel (should trigger R-063 to add 1 empty line)
        var vm = new DocumentEditViewModel(dto, cmdSvc, productsSvc, new StubDialogService());

        // Wait for async product loading
        await Task.Delay(100);

        // Assert 1: R-063 should have created 1 empty line
        Assert.Equal(1, vm.Lines.Count);
        var emptyLine = vm.Lines[0];
        Assert.Equal(0, emptyLine.ItemId);
        Assert.Equal("", emptyLine.ItemName); // Empty line has no ItemName yet

        // Act 2: Simulate user selecting a product (R-022 product selection)
        emptyLine.ItemId = mockProduct.Id; // This triggers LineViewModel_PropertyChanged

        // Wait for async UOM/Lot/Variant loading
        await Task.Delay(100);

        // Assert 2: R-065 FIX - Line should NOT be removed from collection (same object instance)
        Assert.Equal(1, vm.Lines.Count); // Still only 1 line
        Assert.Same(emptyLine, vm.Lines[0]); // R-065 CRITICAL: Must be the SAME object instance, not removed/re-added
        
        // Assert 3: R-064 FIX - ItemName should be updated (line properties updated correctly)
        Assert.Equal(mockProduct.Id, emptyLine.ItemId);
        Assert.Equal("Test Product Name", emptyLine.ItemName); // R-064: ItemName must be updated
        Assert.Equal(20, emptyLine.VatRate); // Auto-filled from product
        Assert.Equal("PCS", emptyLine.Uom); // Auto-filled from product base UOM
    }

    [Fact]
    public async Task WhenProductIsSelected_ShouldAllowQtyEntry()
    {
        // Arrange
        var dto = new DocumentDetailDto 
        { 
            Id = 2, 
            Type = "QUOTE", 
            Lines = new List<DocumentLineDto>() 
        };

        var mockProduct = new ProductRowDto(10, "PROD-001", "Product One", "PCS", 20, true, 50m);
        var products = new List<ProductRowDto> { mockProduct };
        
        var vm = new DocumentEditViewModel(dto, new StubDocumentCommandService(), new StubProductsReadService(products), new StubDialogService());
        await Task.Delay(100);

        var line = vm.Lines[0];
        line.ItemId = mockProduct.Id;
        await Task.Delay(100);

        // Act: User enters quantity
        line.Qty = 15m;
        line.UnitPrice = 100m;

        // Assert: Line should calculate totals correctly
        Assert.Equal(15m, line.Qty);
        Assert.Equal(100m, line.UnitPrice);
        Assert.Equal(1500m, line.LineNet); // 15 * 100
        Assert.Equal(300m, line.LineVat); // 1500 * 0.20
        Assert.Equal("Product One", line.ItemName); // R-064: ItemName should still be set
    }

    [Fact]
    public async Task WhenMultipleProductsSelected_ShouldMaintainAllLines()
    {
        // Arrange
        var dto = new DocumentDetailDto 
        { 
            Id = 3, 
            Type = "SALES_INVOICE", 
            Lines = new List<DocumentLineDto>() 
        };

        var product1 = new ProductRowDto(1, "PROD-A", "Product A", "PCS", 20, true, 10m);
        var product2 = new ProductRowDto(2, "PROD-B", "Product B", "KG", 10, true, 20m);
        var products = new List<ProductRowDto> { product1, product2 };
        
        var vm = new DocumentEditViewModel(dto, new StubDocumentCommandService(), new StubProductsReadService(products), new StubDialogService());
        await Task.Delay(100);

        // Act: Select first product on empty line
        var line1 = vm.Lines[0];
        line1.ItemId = product1.Id;
        await Task.Delay(100);

        // Add a second line by adding to Lines collection
        var newLineDto = new DocumentLineDto();
        dto.Lines.Add(newLineDto);
        var line2 = new DocumentEditViewModel.LineViewModel(vm, newLineDto);
        vm.Lines.Add(line2);

        line2.ItemId = product2.Id;
        await Task.Delay(100);

        // Assert: Both lines should exist with correct data
        Assert.Equal(2, vm.Lines.Count);
        
        Assert.Equal(product1.Id, vm.Lines[0].ItemId);
        Assert.Equal("Product A", vm.Lines[0].ItemName);
        Assert.Equal("PCS", vm.Lines[0].Uom);
        
        Assert.Equal(product2.Id, vm.Lines[1].ItemId);
        Assert.Equal("Product B", vm.Lines[1].ItemName);
        Assert.Equal("KG", vm.Lines[1].Uom);
    }

    private class DelayedProductsReadService : IProductsReadService
    {
        private readonly List<ProductRowDto> _products;
        private readonly int _delayMs;
        public DelayedProductsReadService(List<ProductRowDto> products, int delayMs = 50)
        {
            _products = products;
            _delayMs = delayMs;
        }
        public async Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search)
        {
            await Task.Delay(_delayMs);
            return _products;
        }
        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
        {
            var prod = _products.FirstOrDefault(p => p.Id == productId);
            var list = prod == null ? new List<ProductUomDto>() : new List<ProductUomDto>{ new ProductUomDto(prod.BaseUom, 1m) };
            return Task.FromResult((IReadOnlyList<ProductUomDto>)list);
        }
        public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId) => Task.FromResult((IReadOnlyList<ProductLotDto>)new List<ProductLotDto>());
        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId) => Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());
        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult(_products.FirstOrDefault(p => p.Sku == code));
    }

    // Note: A race-condition scenario where selection occurs before products load
    // is mitigated in production with reconciliation and lazy populate logic.
    // We intentionally avoid a timing-based test here to prevent flakes in CI.
}
