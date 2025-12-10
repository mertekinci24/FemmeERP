using System;
using System.Threading.Tasks;

namespace InventoryERP.Application.EInvoicing;

public interface IEInvoiceAdapter
{
    Task SendInvoiceAsync(int documentId);
    Task<string> GetInvoiceStatusAsync(int documentId);
}
