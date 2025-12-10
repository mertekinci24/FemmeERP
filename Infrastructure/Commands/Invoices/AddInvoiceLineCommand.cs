using MediatR;

namespace InventoryERP.Infrastructure.Commands.Invoices;

public sealed record AddInvoiceLineCommand(int DocId, int ItemId, decimal Qty, decimal UnitPrice, int VatRate, string Uom) : IRequest<Unit>;
