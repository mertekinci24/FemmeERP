using MediatR;

namespace InventoryERP.Infrastructure.Commands.Cash;

public sealed record AllocateCommand(
    int PaymentEntryId,
    int InvoiceEntryId,
    decimal AmountTry
) : IRequest<Unit>;
