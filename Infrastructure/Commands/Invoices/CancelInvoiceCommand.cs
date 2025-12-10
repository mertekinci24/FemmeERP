using MediatR;

namespace InventoryERP.Infrastructure.Commands.Invoices;

public sealed record CancelInvoiceCommand(int DocId, string? Reason) : IRequest<Unit>;
