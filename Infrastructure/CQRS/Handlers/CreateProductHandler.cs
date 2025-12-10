using MediatR;
using InventoryERP.Domain.Entities;
using Persistence;
using InventoryERP.Infrastructure.CQRS.Commands;

namespace InventoryERP.Infrastructure.CQRS.Handlers;

public class CreateProductHandler(AppDbContext db) : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var product = new Product { Sku = cmd.Sku, Name = cmd.Name, BaseUom = cmd.BaseUom, VatRate = cmd.VatRate };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product.Id;
    }
}
