using InventoryERP.Infrastructure.CQRS.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.CQRS.Handlers;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly AppDbContext _db;

    public DeleteProductHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        // R-208: Safe Product Delete
        var hasMoves = await _db.StockMoves.AnyAsync(x => x.ItemId == request.Id, ct);
        if (hasMoves)
        {
            throw new InvalidOperationException("Hareket gören stok silinemez. Pasife alınız.");
        }

        var entity = await _db.Products.FindAsync(new object[] { request.Id }, ct);
        if (entity != null)
        {
            _db.Products.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
        return Unit.Value;
    }
}
