using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Persistence.Services;


public class InventoryQueries : IInventoryQueries
{
    private readonly AppDbContext _db;
    public InventoryQueries(AppDbContext db) => _db = db;

    public async Task<decimal> GetOnHandAsync(int itemId, CancellationToken ct = default)
    {
        var rows = await _db.StockMoves
            .Where(x => x.ItemId == itemId && !x.IsDeleted)
            .Select(x => x.QtySigned)
            .ToListAsync(ct);
        return rows.Sum();
    }

    public async Task<decimal> GetPartnerBalanceAsync(int partnerId, CancellationToken ct = default)
    {
        var debit = (await _db.PartnerLedgerEntries
            .Where(x => x.PartnerId == partnerId && !x.IsDeleted)
            .Select(x => x.Debit)
            .ToListAsync(ct))
            .Sum();

        var credit = (await _db.PartnerLedgerEntries
            .Where(x => x.PartnerId == partnerId && !x.IsDeleted)
            .Select(x => x.Credit)
            .ToListAsync(ct))
            .Sum();
        return debit - credit;
    }

    public async Task<decimal> GetAvailableAsync(int itemId, CancellationToken ct = default)
    {
        var onHand = await GetOnHandAsync(itemId, ct);
        var reserved = await _db.Products.Where(p => p.Id == itemId && !p.IsDeleted).Select(p => (decimal?)p.ReservedQty).FirstOrDefaultAsync(ct) ?? 0m;
        return onHand - reserved;
    }
}

