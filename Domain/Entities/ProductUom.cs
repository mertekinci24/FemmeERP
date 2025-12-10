using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class ProductUom : EntityBase
{
    public int ProductId { get; set; }
    public string UomName { get; set; } = null!; // e.g. "KOLI"
    public decimal Coefficient { get; set; } = 1m; // multiplier to base UOM

    public Product? Product { get; set; }
}
