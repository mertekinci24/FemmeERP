using FluentAssertions;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Tests.Unit.TestHelpers;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Unit
{
    /// <summary>
    /// TST-014: Unit tests for ExportPdfCommand.CanExecute logic
    /// Verifies business rule: PDF export only enabled for saved Quote documents (Id > 0)
    /// </summary>
    public class ExportPdfCanExecuteTests
    {
        // Stub implementations for testing
        private sealed class StubCmd : Application.Documents.IDocumentCommandService
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

        private sealed class StubProducts : IProductsReadService
        {
            public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search) => 
                Task.FromResult<IReadOnlyList<ProductRowDto>>(new List<ProductRowDto>());
            public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId) => 
                Task.FromResult<IReadOnlyList<ProductUomDto>>(new List<ProductUomDto>());
            public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId) => 
                Task.FromResult<IReadOnlyList<ProductLotDto>>(new List<ProductLotDto>());
            public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId) => 
                Task.FromResult<IReadOnlyList<ProductVariantDto>>(new List<ProductVariantDto>());
            public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
        }

        [Fact]
        public void ExportPdfCommand_CanExecute_Returns_False_For_Unsaved_Document()
        {
            // Arrange: Create DTO for new unsaved Quote document (Id = 0)
            var dto = new DocumentDetailDto
            {
                Id = 0, // NEW DOCUMENT - not saved
                Type = "QUOTE",
                Lines = new List<DocumentLineDto>()
            };

            // Create ViewModel with minimal stubs
            var viewModel = new DocumentEditViewModel(
                dto: dto,
                cmd: new StubCmd(),
                productsSvc: new StubProducts(),
                dialogService: new StubDialogService());

            // Act: Check if ExportPdfCommand can execute
            bool canExecute = viewModel.ExportPdfCommand.CanExecute(null);

            // Assert: Should return FALSE because document is not saved (Id = 0)
            canExecute.Should().BeFalse("PDF export should be disabled for unsaved documents (Id = 0)");
        }

        [Fact]
        public void ExportPdfCommand_CanExecute_Returns_True_For_Saved_Quote_Document()
        {
            // Arrange: Create DTO for saved Quote document (Id > 0)
            var dto = new DocumentDetailDto
            {
                Id = 1, // SAVED DOCUMENT
                Type = "QUOTE",
                Lines = new List<DocumentLineDto>()
            };

            // Create ViewModel with minimal stubs
            var viewModel = new DocumentEditViewModel(
                dto: dto,
                cmd: new StubCmd(),
                productsSvc: new StubProducts(),
                dialogService: new StubDialogService());

            // Act: Check if ExportPdfCommand can execute
            bool canExecute = viewModel.ExportPdfCommand.CanExecute(null);

            // Assert: Should return TRUE because document is saved (Id > 0) and is a Quote
            canExecute.Should().BeTrue("PDF export should be enabled for saved Quote documents (Id > 0)");
        }

        [Fact]
        public void ExportPdfCommand_CanExecute_Returns_False_For_Non_Quote_Document()
        {
            // Arrange: Create DTO for saved non-Quote document (INVOICE)
            var dto = new DocumentDetailDto
            {
                Id = 1, // Saved
                Type = "SALES_INVOICE", // Not a Quote
                Lines = new List<DocumentLineDto>()
            };

            // Create ViewModel with minimal stubs
            var viewModel = new DocumentEditViewModel(
                dto: dto,
                cmd: new StubCmd(),
                productsSvc: new StubProducts(),
                dialogService: new StubDialogService());

            // Act: Check if ExportPdfCommand can execute
            bool canExecute = viewModel.ExportPdfCommand.CanExecute(null);

            // Assert: Should return FALSE because document is not a Quote
            canExecute.Should().BeFalse("PDF export should only be enabled for Quote documents");
        }
    }
}
