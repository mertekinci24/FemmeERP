// ReSharper disable once All
#nullable enable
using System.Threading.Tasks;

namespace InventoryERP.Presentation.Abstractions
{
    /// <summary>
    /// R-037: Abstraction for dialog and message box operations.
    /// Allows ViewModels to remain testable without direct WPF dependencies.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a simple message box with a message.
        /// </summary>
        void ShowMessageBox(string message, string title = "Bilgi");

        /// <summary>
        /// Shows stock information for a product.
        /// </summary>
        /// <param name="sku">Product SKU</param>
        /// <param name="name">Product name</param>
        /// <param name="baseUom">Base unit of measure</param>
        /// <param name="onHandQty">On-hand quantity</param>
        void ShowStockInfo(string sku, string name, string baseUom, decimal onHandQty);

        /// <summary>
        /// Shows the adjustment document editor dialog.
        /// </summary>
        /// <param name="documentId">The document ID to edit</param>
        /// <returns>True if user confirmed/saved, false if cancelled</returns>
        Task<bool> ShowAdjustmentDialogAsync(int documentId);

        /// <summary>
        /// Shows the stock movements dialog for a product.
        /// </summary>
        /// <param name="productId">The product ID</param>
        Task ShowStockMovementsAsync(int productId);

        /// <summary>
        /// Shows the document edit dialog for any document type.
        /// R-042: Generic method to replace ShowDialogSafe pattern.
        /// </summary>
        /// <param name="documentId">The document ID to edit</param>
        /// <returns>True if user confirmed/saved, false if cancelled</returns>
        Task<bool> ShowDocumentEditDialogAsync(int documentId);

        /// <summary>
        /// Shows the cash receipt dialog (Tahsilat FiÅŸi).
        /// R-131: Opens dialog for creating cash receipt.
        /// </summary>
        Task<bool> ShowCashReceiptDialogAsync();

        /// <summary>
        /// Shows the cash payment dialog (Ã–deme FiÅŸi).
        /// R-131: Opens dialog for creating cash payment.
        /// </summary>
        Task<bool> ShowCashPaymentDialogAsync();

        /// <summary>
        /// Shows the item (product) edit dialog with 4 tabs.
        /// R-043: Opens ItemEditDialog for creating new or editing existing products.
        /// </summary>
        /// <param name="productId">The product ID to edit, or null for new product</param>
        /// <returns>True if user confirmed/saved, false if cancelled</returns>
        Task<bool> ShowItemEditDialogAsync(int? productId);

        /// <summary>
        /// R-108: Show detailed error information asynchronously (restores R-098 strategy diagnostics).
        /// Displays a dialog with full exception details (including stack trace).
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="details">Full error details (ex.ToString())</param>
        Task ShowErrorAsync(string title, string details);
    }
}
