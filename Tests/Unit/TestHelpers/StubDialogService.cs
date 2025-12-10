using System.Threading.Tasks;
using InventoryERP.Presentation.Abstractions;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;

namespace Tests.Unit.TestHelpers;

/// <summary>
/// R-069: Stub IDialogService for tests - doesn't show actual dialogs
/// </summary>
public class StubDialogService : IDialogService
{
    public string? LastTitle { get; private set; }
    public string? LastMessage { get; private set; }
    public int CallCount { get; private set; }

    public void ShowMessageBox(string message, string title = "Bilgi")
    {
        LastTitle = title;
        LastMessage = message;
        CallCount++;
    }

    public void ShowStockInfo(string sku, string name, string baseUom, decimal onHandQty)
    {
        // No-op for tests
    }

    public Task<bool> ShowAdjustmentDialogAsync(int documentId) => Task.FromResult(false);
    public Task ShowStockMovementsAsync(int productId) => Task.CompletedTask;
    public Task<bool> ShowDocumentEditDialogAsync(int documentId) => Task.FromResult(false);
    public Task<bool> ShowCashReceiptDialogAsync() => Task.FromResult(false);
    public Task<bool> ShowCashPaymentDialogAsync() => Task.FromResult(false);
    public Task<bool> ShowItemEditDialogAsync(int? productId) => Task.FromResult(false);

    public Task ShowErrorAsync(string title, string details)
    {
        LastTitle = title;
        LastMessage = details;
        CallCount++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// R-103 (R-101): Stub services for PDF export - required by DocumentEditViewModel constructor
/// </summary>
public class StubReportService : IDocumentReportService
{
    public Task<byte[]> GenerateQuotePdfAsync(int documentId) => Task.FromResult(new byte[0]);
    public Task<byte[]> BuildInvoicePdfAsync(int id) => Task.FromResult(new byte[0]);
    public Task<byte[]> ExportListExcelAsync(DocumentListFilter filter) => Task.FromResult(new byte[0]);
    public Task<byte[]> ExportListPdfAsync(DocumentListFilter filter) => Task.FromResult(new byte[0]);
}

public class StubFileDialogService : IFileDialogService
{
    public string? ShowSaveFileDialog(string defaultFileName, string filter, string title) => null;
    public string? ShowOpenFileDialog(string filter, string title) => null;
}
