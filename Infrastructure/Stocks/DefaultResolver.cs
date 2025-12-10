using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Stocks;
using InventoryERP.Domain.Wms;

namespace InventoryERP.Infrastructure.Stocks;

public sealed class DefaultResolver : IDefaultResolver
{
    private readonly Func<IWmsRepository> _repoFactory;

    public DefaultResolver(Func<IWmsRepository> repoFactory)
    {
        _repoFactory = repoFactory;
    }

    public async Task<(Warehouse warehouse, Location location)> ResolveAsync(int? productId)
    {
        var repo = _repoFactory();

        // 1) Try product defaults
        if (productId.HasValue)
        {
            var prodDefault = await repo.GetProductDefaultAsync(productId.Value);
            if (prodDefault is not null)
            {
                return prodDefault.Value;
            }
        }

        // 2) System default
        var sysDefault = await repo.GetSystemDefaultAsync();
        if (sysDefault is not null)
            return sysDefault.Value;

        // 3) Fallback to UNASSIGNED
        var fall = await repo.GetUnassignedAsync();
        if (fall is not null)
            return fall.Value;

        // hard failure should never occur if seeding ran; as ultimate guard, create synthetic in-memory stubs
        var wh = new Warehouse { Id = 0, Code = "MAIN", Name = "MAIN", IsDefault = true, IsActive = true };
        var loc = new Location { Id = 0, WarehouseId = 0, Code = "UNASSIGNED", Name = "UNASSIGNED", IsDefault = false, IsActive = true, VisibleInUI = false };
        return (wh, loc);
    }
}

public interface IWmsRepository
{
    Task<(Warehouse wh, Location loc)?> GetProductDefaultAsync(int productId);
    Task<(Warehouse wh, Location loc)?> GetSystemDefaultAsync();
    Task<(Warehouse wh, Location loc)?> GetUnassignedAsync();
}

