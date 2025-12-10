using System.Threading.Tasks;
using InventoryERP.Domain.Wms;

namespace InventoryERP.Application.Stocks;

public interface IDefaultResolver
{
    Task<(Warehouse warehouse, Location location)> ResolveAsync(int? productId);
}

