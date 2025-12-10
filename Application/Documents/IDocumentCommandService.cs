using InventoryERP.Application.Documents.DTOs;
using System.Threading.Tasks;

namespace InventoryERP.Application.Documents
{
    public interface IDocumentCommandService
    {
        Task<int> CreateDraftAsync(DocumentDetailDto dto);
        Task UpdateDraftAsync(int id, DocumentDetailDto dto);
        Task DeleteDraftAsync(int id);
        Task ApproveAsync(int id);
        Task CancelAsync(int id);
        // R-057: Single transaction method for adjustment documents (save + approve atomically)
        Task SaveAndApproveAdjustmentAsync(int id, DocumentDetailDto dto);
        // Convert an approved SALES_ORDER into a draft SEVK_IRSALIYESI and return the new document id
        Task<int> ConvertSalesOrderToDispatchAsync(int salesOrderId);
    // Convert an approved SEVK_IRSALIYESI into a draft SALES_INVOICE and return the new document id
    Task<int> ConvertDispatchToInvoiceAsync(int dispatchId);
    }
}
