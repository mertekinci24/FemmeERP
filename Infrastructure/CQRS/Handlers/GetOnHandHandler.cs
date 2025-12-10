using MediatR;
using Persistence;
using Microsoft.EntityFrameworkCore;
using InventoryERP.Infrastructure.CQRS.Queries;

namespace InventoryERP.Infrastructure.CQRS.Handlers;

public class GetOnHandHandler(AppDbContext db) : IRequestHandler<GetOnHandQuery, decimal>
{
    public async Task<decimal> Handle(GetOnHandQuery query, CancellationToken ct)
    {
        var rows = await db.StockMoves
            .Where(x => x.ItemId == query.ItemId)
            .Select(x => x.QtySigned)
            .ToListAsync(ct);
        return rows.Sum();
    }
}
