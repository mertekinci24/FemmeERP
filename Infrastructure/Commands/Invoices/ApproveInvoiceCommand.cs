using MediatR;

namespace InventoryERP.Infrastructure.Commands.Invoices;

public sealed record ApproveInvoiceCommand(int DocId, string? ExternalId, string? Number) : IRequest<Unit>;
