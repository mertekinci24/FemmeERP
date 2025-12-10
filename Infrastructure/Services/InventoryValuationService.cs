using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Stocks;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Services;

public sealed class InventoryValuationService : IInventoryValuationService
{
    private readonly AppDbContext _db;
    public InventoryValuationService(AppDbContext db) => _db = db;

    public async Task<decimal> GetTotalInventoryValueAsync(DateTime asOfDate)
    {
        var cutoff = asOfDate.Date.AddDays(1).AddTicks(-1); // end of day

        // SQLite cannot translate Sum(decimal) reliably, so materialize before aggregation
        var moves = await _db.StockMoves
            .Where(m => m.Date <= cutoff)
            .Select(m => new { m.ItemId, m.QtySigned })
            .ToListAsync();

        var qtyPerItem = moves
            .GroupBy(m => m.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.QtySigned) })
            .Where(r => r.Qty > 0)
            .ToList();

        if (qtyPerItem.Count == 0)
        {
            return 0m;
        }

        var itemIds = qtyPerItem.Select(r => r.ItemId).ToList();
        var productCosts = await _db.Products
            .Where(p => itemIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Cost })
            .ToDictionaryAsync(p => p.Id, p => p.Cost);

        decimal total = 0m;
        foreach (var row in qtyPerItem)
        {
            if (!productCosts.TryGetValue(row.ItemId, out var mwa)) continue;
            total += Math.Round(row.Qty * mwa, 2, MidpointRounding.AwayFromZero);
        }
        return total;
    }
}
