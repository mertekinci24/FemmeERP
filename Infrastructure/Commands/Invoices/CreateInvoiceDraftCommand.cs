using MediatR;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Infrastructure.Commands.Invoices;

public sealed record CreateInvoiceDraftCommand(int PartnerId, DocumentType Type, string Currency, decimal? FxRate) : IRequest<Unit>;
