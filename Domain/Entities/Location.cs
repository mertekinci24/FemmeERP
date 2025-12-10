using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class Location : EntityBase
{
    public int WarehouseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public Warehouse? Warehouse { get; set; }
}
