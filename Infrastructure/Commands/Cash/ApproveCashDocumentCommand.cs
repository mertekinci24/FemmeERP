using MediatR;

namespace InventoryERP.Infrastructure.Commands.Cash;

public sealed record ApproveCashDocumentCommand(
    int DocId,
    string? Number
) : IRequest<Unit>;
