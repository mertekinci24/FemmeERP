using MediatR;

namespace InventoryERP.Infrastructure.CQRS.Commands;

public record CreateProductCommand(string Sku, string Name, string BaseUom, int VatRate) : IRequest<int>;
