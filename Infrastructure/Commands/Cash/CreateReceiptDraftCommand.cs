using MediatR;

namespace InventoryERP.Infrastructure.Commands.Cash;

public sealed record CreateReceiptDraftCommand(
    int PartnerId,
    DateTime Date,
    string Currency,
    decimal? FxRate,
    string? ExternalId
) : IRequest<int>;
