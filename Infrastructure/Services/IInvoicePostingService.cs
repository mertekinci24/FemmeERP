using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Infrastructure.Commands.Invoices;

namespace InventoryERP.Infrastructure.Services;

public interface IInvoicePostingService
{
    Task<int> CreateDraftAsync(CreateInvoiceDraftCommand command, CancellationToken ct);
    Task<int> AddLineAsync(AddInvoiceLineCommand command, CancellationToken ct);
    Task ApproveAsync(ApproveInvoiceCommand command, CancellationToken ct);
}
