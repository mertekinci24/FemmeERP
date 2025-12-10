using InventoryERP.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Persistence.Services;

public class InventoryQueriesEf : IInventoryQueries
{
    private readonly AppDbContext _db;
    public InventoryQueriesEf(AppDbContext db) => _db = db;
    public async Task<decimal> GetOnHandAsync(int itemId, CancellationToken ct = default)
    {
        var list = await _db.StockMoves
            .Where(x => x.ItemId == itemId)
            .Select(x => x.QtySigned)
            .ToListAsync(ct);
        return list.Sum();
    }

    public async Task<decimal> GetAvailableAsync(int itemId, CancellationToken ct = default)
    {
        var onHand = await GetOnHandAsync(itemId, ct);
        var reserved = await _db.Products.Where(p => p.Id == itemId).Select(p => (decimal?)p.ReservedQty).FirstOrDefaultAsync(ct) ?? 0m;
        return onHand - reserved;
    }

    public async Task<decimal> GetPartnerBalanceAsync(int partnerId, CancellationToken ct = default)
    {
        var debitList = await _db.PartnerLedgerEntries
            .Where(x => x.PartnerId == partnerId)
            .Select(x => x.Debit)
            .ToListAsync(ct);
        var debit = debitList.Sum();

        var creditList = await _db.PartnerLedgerEntries
            .Where(x => x.PartnerId == partnerId)
            .Select(x => x.Credit)
            .ToListAsync(ct);
        var credit = creditList.Sum();
        return debit - credit;
    }
}
