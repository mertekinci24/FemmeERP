using MediatR;

namespace InventoryERP.Infrastructure.Commands.Cash;

public sealed record AutoAllocateOldestCommand(
    int PartnerId,
    decimal? AmountTryHint = null
) : IRequest<int>;
