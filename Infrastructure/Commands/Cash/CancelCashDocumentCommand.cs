using MediatR;

namespace InventoryERP.Infrastructure.Commands.Cash;

public sealed record CancelCashDocumentCommand(
    int DocId,
    string? Reason
) : IRequest<Unit>;
