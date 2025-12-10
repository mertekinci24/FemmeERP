using MediatR;

namespace InventoryERP.Infrastructure.CQRS.Queries;

public record GetOnHandQuery(int ItemId) : IRequest<decimal>;
