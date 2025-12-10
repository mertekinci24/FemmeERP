using System;
using System.Threading.Tasks;

namespace InventoryERP.Application.Stocks;

public interface IInventoryValuationService
{
    Task<decimal> GetTotalInventoryValueAsync(DateTime asOfDate);
}
