using MediatR;

namespace InventoryERP.Infrastructure.Commands.Cash;

public sealed record DeallocateCommand(
    int AllocationId
) : IRequest<Unit>;
