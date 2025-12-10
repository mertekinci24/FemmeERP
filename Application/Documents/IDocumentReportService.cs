using InventoryERP.Application.Documents.DTOs;
using System.Threading.Tasks;

namespace InventoryERP.Application.Documents
{
    public interface IDocumentReportService
    {
        Task<byte[]> BuildInvoicePdfAsync(int id);
        Task<byte[]> ExportListExcelAsync(DocumentListFilter filter);
        Task<byte[]> ExportListPdfAsync(DocumentListFilter filter);
        // R-008: Generate PDF export for Quote (Teklif) documents
        Task<byte[]> GenerateQuotePdfAsync(int documentId);
    }
}
