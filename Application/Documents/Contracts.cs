using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using InventoryERP.Application.Documents.DTOs;

namespace InventoryERP.Application.Documents;

// R-245: Added WarehouseName and Description for list columns (Bug 5)
public sealed record DocumentRowDto(
    int Id, string Number, string Type, DateTime Date,
    string Partner, string Status, decimal NetTry, decimal VatTry, decimal GrossTry,
    string? WarehouseName = null, string? Description = null);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);

public interface IDocumentQueries
{
    Task<PagedResult<DocumentRowDto>> ListAsync(DocumentListFilter filter, int page, int pageSize);
    Task<DocumentDetailDto?> GetAsync(int id);
}

public sealed record CreateInvoiceDraftDto(string Type, DateTime Date);
public sealed record ApproveInvoiceDto(int DocumentId);
public sealed record CancelInvoiceDto(int DocumentId);

public interface IInvoiceCommandService
{
    Task<int> CreateDraftAsync(CreateInvoiceDraftDto cmd);
    Task ApproveAsync(ApproveInvoiceDto cmd);
    Task CancelAsync(CancelInvoiceDto cmd);
}
