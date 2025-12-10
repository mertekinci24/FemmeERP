using System.Threading.Tasks;
using InventoryERP.Application.EInvoicing;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Adapters;

public sealed class MockEInvoiceAdapter : IEInvoiceAdapter
{
    private readonly AppDbContext _db;
    public MockEInvoiceAdapter(AppDbContext db) => _db = db;

    public async Task SendInvoiceAsync(int documentId)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc == null) return;
        doc.Status = DocumentStatus.SENT;
        await _db.SaveChangesAsync();
    }

    public async Task<string> GetInvoiceStatusAsync(int documentId)
    {
        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId);
        return doc == null ? "NOT_FOUND" : doc.Status.ToString();
    }
}
