using System.Threading.Tasks;
using InventoryERP.Presentation.Abstractions;

namespace Tests.Infrastructure
{
    /// <summary>
    /// R-037: Fake/no-op implementation of IDialogService for unit tests.
    /// All methods do nothing and return immediately to avoid blocking test threads.
    /// </summary>
    public class FakeDialogService : IDialogService
    {
        public void ShowMessageBox(string message, string title = "Bilgi")
        {
            // No-op: don't show UI in tests
        }

        public void ShowStockInfo(string sku, string name, string baseUom, decimal onHandQty)
        {
            // No-op: don't show UI in tests
        }

        public Task<bool> ShowAdjustmentDialogAsync(int documentId)
        {
            // No-op: return success immediately
            return Task.FromResult(true);
        }

        public Task ShowStockMovementsAsync(int productId)
        {
            // No-op: return completed task immediately
            return Task.CompletedTask;
        }

        public Task<bool> ShowDocumentEditDialogAsync(int documentId)
        {
            // R-042: No-op for tests - return success immediately
            return Task.FromResult(true);
        }

        public Task<bool> ShowCashReceiptDialogAsync()
        {
            // R-131: No-op cash receipt dialog - always succeeds in tests
            return Task.FromResult(true);
        }

        public Task<bool> ShowCashPaymentDialogAsync()
        {
            // R-131: No-op cash payment dialog - always succeeds in tests
            return Task.FromResult(true);
        }

        public Task<bool> ShowItemEditDialogAsync(int? productId)
        {
            // R-043: No-op for tests - return success immediately
            return Task.FromResult(true);
        }

        public Task ShowErrorAsync(string title, string details)
        {
            // No-op: return completed task
            return Task.CompletedTask;
        }
    }
}
