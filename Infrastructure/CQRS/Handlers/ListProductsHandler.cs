using MediatR;
using Persistence;
using InventoryERP.Infrastructure.CQRS.Queries;
using Microsoft.EntityFrameworkCore;

namespace InventoryERP.Infrastructure.CQRS.Handlers;

public class ListProductsHandler(AppDbContext db) : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(ListProductsQuery query, CancellationToken ct)
    {
        var q = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(x => x.Name.Contains(query.Search) || x.Sku.Contains(query.Search));
        var items = await q.OrderBy(x => x.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ProductDto(x.Id, x.Sku, x.Name, x.BaseUom, x.VatRate, x.Active))
            .ToListAsync(ct);
        return items;
    }
}
