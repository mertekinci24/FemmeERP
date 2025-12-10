using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class BomItem : EntityBase
{
    public int ParentProductId { get; set; }
    public int ComponentProductId { get; set; }
    public decimal QtyPer { get; set; }

    public Product? ParentProduct { get; set; }
    public Product? ComponentProduct { get; set; }
}
