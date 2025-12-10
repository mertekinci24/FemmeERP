using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class Lot : EntityBase
{
    public int ProductId { get; set; }
    public string LotNumber { get; set; } = null!;
    public DateTime? ExpiryDate { get; set; }

    public Product? Product { get; set; }
}
