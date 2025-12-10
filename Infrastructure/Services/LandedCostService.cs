using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Services;

public interface ILandedCostService
{
    Task ApplyAsync(int purchaseInvoiceId, IReadOnlyList<int> goodsReceiptDocumentIds, CancellationToken ct = default);
}

public sealed class LandedCostService : ILandedCostService
{
    private readonly AppDbContext _db;
    public LandedCostService(AppDbContext db) => _db = db;

    public async Task ApplyAsync(int purchaseInvoiceId, IReadOnlyList<int> goodsReceiptDocumentIds, CancellationToken ct = default)
    {
        if (goodsReceiptDocumentIds == null || goodsReceiptDocumentIds.Count == 0) throw new ArgumentException("No target documents.");

        var inv = await _db.Documents.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == purchaseInvoiceId, ct)
                  ?? throw new InvalidOperationException("INV-404");
        if (inv.Type != DocumentType.PURCHASE_INVOICE) throw new InvalidOperationException("INV-NOT-PURCHASE");

        // Compute amount in TRY
        decimal amountTry;
        if (inv.Lines != null && inv.Lines.Count > 0)
        {
            var totals = InventoryERP.Domain.Services.DocumentCalculator.ComputeTotals(inv.Lines);
            var fx = inv.FxRate ?? 1m;
            amountTry = Math.Round(totals.Gross * fx, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            amountTry = inv.TotalTry;
        }
        if (amountTry <= 0m) throw new InvalidOperationException("INV-AMOUNT-0");

        var recs = await _db.Documents
            .Include(d => d.Lines)
            .Where(d => goodsReceiptDocumentIds.Contains(d.Id))
            .ToListAsync(ct);
        if (recs.Any(d => d.Type != DocumentType.GELEN_IRSALIYE)) throw new InvalidOperationException("DOC-NOT-GR");

        var allLines = recs.SelectMany(d => d.Lines).ToList();
        var totalQty = allLines.Sum(l => l.Qty * l.Coefficient);
        if (totalQty <= 0m) throw new InvalidOperationException("GR-QTY-0");

        var addPerUnit = Math.Round(amountTry / totalQty, 6, MidpointRounding.AwayFromZero);

        // Update stock moves and product MWA
        // For each line, find its inbound stock move
        foreach (var line in allLines)
        {
            var move = await _db.StockMoves.FirstOrDefaultAsync(m => m.DocLineId == line.Id && m.QtySigned > 0, ct)
                       ?? throw new InvalidOperationException("MOVE-404");
            var baseUnit = line.UnitPrice; // fallback if prior is null
            move.UnitCost = (move.UnitCost ?? baseUnit) + addPerUnit;

            var prod = await _db.Products.FirstAsync(p => p.Id == line.ItemId, ct);
            // Approximate: current MWA increases by addPerUnit when all remaining inventory came from these receipts
            prod.Cost = Math.Round(prod.Cost + addPerUnit, 6, MidpointRounding.AwayFromZero);
        }

        await _db.SaveChangesAsync(ct);
    }
}
