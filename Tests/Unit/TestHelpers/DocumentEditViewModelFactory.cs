using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Application.Products;
using InventoryERP.Presentation.ViewModels;
using Microsoft.Extensions.Options;
using InventoryERP.Presentation.Configuration;

namespace Tests.Unit.TestHelpers
{
    /// <summary>
    /// R-103: Factory to simplify DocumentEditViewModel creation in tests
    /// Automatically provides stub PDF export services required by R-101
    /// </summary>
    public static class DocumentEditViewModelFactory
    {
        public static DocumentEditViewModel Create(
            DocumentDetailDto dto,
            IDocumentCommandService cmd,
            IProductsReadService productsSvc,
            InventoryERP.Presentation.Abstractions.IDialogService dialogService,
            Application.Partners.IPartnerService? partnerService = null,
            IOptions<UiOptions>? uiOptions = null)
        {
            return new DocumentEditViewModel(
                dto,
                cmd,
                productsSvc,
                dialogService,
                new StubReportService(),
                new StubFileDialogService(),
                partnerService,
                uiOptions);
        }
    }
}
