using InventoryERP.Domain.Interfaces;
using Persistence;
using InventoryERP.Domain.Entities;

namespace InventoryERP.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly AppDbContext _db;
    private readonly IInventoryQueries _q;
    public StockService(AppDbContext db, IInventoryQueries q){ _db=db; _q=q; }

    public async Task PostMoveAsync(int itemId, decimal qty, bool isInbound, string? note = null, CancellationToken ct = default)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        var signed = isInbound ? qty : -qty;
        if (!isInbound)
        {
            var onHand = await _q.GetOnHandAsync(itemId, ct);
            if (onHand + signed < 0) throw new InvalidOperationException("STK-NEG-001");
        }
        _db.StockMoves.Add(new StockMove { ItemId = itemId, QtySigned = signed, Date = DateTime.UtcNow, Note = note });
        await _db.SaveChangesAsync(ct);
    }
}
