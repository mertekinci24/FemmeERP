using MediatR;
using InventoryERP.Domain.Services;
using Persistence;
using InventoryERP.Infrastructure.Services;
using InventoryERP.Infrastructure.CQRS.Commands;

namespace InventoryERP.Infrastructure.CQRS.Handlers;

public class PostStockMoveHandler(AppDbContext db) : IRequestHandler<PostStockMoveCommand, int>
{
    public async Task<int> Handle(PostStockMoveCommand cmd, CancellationToken ct)
    {
        var queries = new InventoryERP.Persistence.Services.InventoryQueries(db);
        var service = new Infrastructure.Services.StockService(db, queries);
        bool isInbound = cmd.Direction == "in";
        await service.PostMoveAsync(cmd.ItemId, cmd.Qty, isInbound);
        return cmd.ItemId;
    }
}
