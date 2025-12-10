using MediatR;

namespace InventoryERP.Infrastructure.CQRS.Queries;

public record ListProductsQuery(string? Search, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<ProductDto>>;

public record ProductDto(int Id, string Sku, string Name, string BaseUom, int VatRate, bool Active);
