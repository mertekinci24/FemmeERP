using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class Warehouse : EntityBase
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsDefault { get; set; }

    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
