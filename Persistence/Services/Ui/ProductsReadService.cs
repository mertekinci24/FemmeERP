using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Persistence.Services.Ui;

public record ProductRow(int Id, string Sku, string Name, string BaseUom, int VatRate, bool Active);

public interface IProductsReadService
{
    Task<List<ProductRow>> ListAsync(int take = 50, CancellationToken ct = default);
}

public sealed class ProductsReadService(AppDbContext db) : IProductsReadService
{
    public async Task<List<ProductRow>> ListAsync(int take = 50, CancellationToken ct = default)
        => await db.Set<InventoryERP.Domain.Entities.Product>()
            .OrderBy(p => p.Id)
            .Take(take)
            .Select(p => new ProductRow(p.Id, p.Sku, p.Name, p.BaseUom, p.VatRate, p.Active))
            .ToListAsync(ct);
}
