using MediatR;

namespace InventoryERP.Infrastructure.CQRS.Commands;

public record PostStockMoveCommand(int ItemId, decimal Qty, string Direction) : IRequest<int>;
