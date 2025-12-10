using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using Xunit;

namespace Tests.Unit;

/// <summary>
/// R-069: Test that exceptions in product selection trigger diagnostic dialog
/// Proves IDialogService.ShowMessageBox works when exception occurs (R-052 strategy)
/// </summary>
public class R069_DiagnosticDialogTest
{
    private class StubDocumentCommandService : IDocumentCommandService
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

    private class ThrowingProductsReadService : IProductsReadService
    {
        public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search) => 
            Task.FromResult((IReadOnlyList<ProductRowDto>)new List<ProductRowDto>
            {
                // Return a product so FirstOrDefault finds it
                new ProductRowDto(999, "TEST-SKU", "Test Product", "PCS", 18, true, 100m)
            });

        public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
        {
            // R-069: Simulate exception during UOM loading (triggered by product selection)
            throw new InvalidOperationException("R-069 TEST: Simulated UOM loading failure");
        }

        public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId) => 
            Task.FromResult((IReadOnlyList<ProductLotDto>)new List<ProductLotDto>());

        public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId) => 
            Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());

        public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
    }

    [Fact]
    public async Task WhenProductSelectionThrowsException_ShouldShowDiagnosticDialog()
    {
        // Arrange: Create QUOTE document with empty lines (triggers R-063 empty line creation)
        var dto = new DocumentDetailDto 
        { 
            Id = 1, 
            Type = "QUOTE", 
            Lines = new List<DocumentLineDto>() 
        };

        var stubDialog = new StubDialogService();
        var vm = new DocumentEditViewModel(
            dto, 
            new StubDocumentCommandService(), 
            new ThrowingProductsReadService(), 
            stubDialog);

        // Wait for R-063 empty line creation
        await Task.Delay(100);

        // Assert precondition: Should have 1 empty line from R-063
        Assert.Equal(1, vm.Lines.Count);

        // Act: Trigger product selection (will throw exception in LoadUomsForLineAsync)
        var line = vm.Lines[0];
        line.ItemId = 999; // This triggers LineViewModel_PropertyChanged › LoadUomsForLineAsync › throws

        // Wait for async PropertyChanged handler to complete
        await Task.Delay(300);

        // Assert: Dialog should have been called with diagnostic info
        Assert.Equal(1, stubDialog.CallCount);
        Assert.Equal("R-069 DIAGNOSTIC", stubDialog.LastTitle);
        Assert.NotNull(stubDialog.LastMessage);
        Assert.Contains("R-069 TEST", stubDialog.LastMessage);
        Assert.Contains("InvalidOperationException", stubDialog.LastMessage);
        Assert.Contains("Simulated UOM loading failure", stubDialog.LastMessage);
    }
}
