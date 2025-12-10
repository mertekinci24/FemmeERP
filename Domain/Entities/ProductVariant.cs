using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class ProductVariant : EntityBase
{
    public int ProductId { get; set; }
    public string Code { get; set; } = null!; // e.g., SKU-RED-L

    public Product? Product { get; set; }
}
