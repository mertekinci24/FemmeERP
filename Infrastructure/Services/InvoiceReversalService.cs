using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryERP.Infrastructure.Services;

public sealed class InvoiceReversalService
{
    private readonly AppDbContext _db;

    public InvoiceReversalService(AppDbContext db)
    {
        _db = db;
    }

    public async Task ReverseAsync(int docId, string? reason, CancellationToken ct)
    {
        using var tx = await _db.Database.BeginTransactionAsync(ct);

        var doc = await _db.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == docId, ct)
                  ?? throw new InvalidOperationException("DOC-404");
    if (doc.Status != DocumentStatus.POSTED) return;

        // Reverse behavior depends on document type
        if (doc.Type == DocumentType.SALES_ORDER)
        {
            // decrease reserved qty
            foreach (var l in doc.Lines)
            {
                var prod = await _db.Products.FindAsync(new object[] { l.ItemId }, ct) ?? throw new InvalidOperationException("ITEM-404");
                prod.ReservedQty -= l.Qty;
            }
        }
        else
        {
            // Reverse stock moves
            foreach (var l in doc.Lines)
            {
                var origSigned = doc.Type == DocumentType.SALES_INVOICE ? -l.Qty : +l.Qty;
                _db.StockMoves.Add(new StockMove { ItemId = l.ItemId, QtySigned = -origSigned, Date = DateTime.UtcNow, DocLineId = l.Id, Note = reason });
            }

            // Reverse ledger entry
            var origEntry = await _db.PartnerLedgerEntries.FirstOrDefaultAsync(e => e.DocId == doc.Id, ct);
            if (origEntry != null)
            {
                _db.PartnerLedgerEntries.Add(new PartnerLedgerEntry {
                    PartnerId = origEntry.PartnerId, Date = DateTime.UtcNow, DueDate = origEntry.DueDate,
                    Currency = origEntry.Currency, FxRate = origEntry.FxRate,
                    Debit = origEntry.Credit, Credit = origEntry.Debit,
                    AmountTry = origEntry.AmountTry, Status = LedgerStatus.CANCELED,
                    DocId = doc.Id, DocType = doc.Type, DocNumber = doc.Number
                });
            }
        }

        doc.Status = DocumentStatus.CANCELED;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
