using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class Product : EntityBase
{
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string BaseUom { get; set; } = null!;
    public string? Barcode { get; set; }
    // reserved quantity held for confirmed but not yet shipped orders
    public decimal ReservedQty { get; set; }
    public int VatRate { get; set; }
    public bool Active { get; set; } = true;
    // Moving Weighted Average (MWA) cost
    public decimal Cost { get; set; }
    // R-234: Sales price for invoicing
    public decimal SalesPrice { get; set; }
    // R-033: Product category for grouping/reporting
    public string? Category { get; set; }
    // R-274: Product brand (Marka)
    public string? Brand { get; set; }
    
    // R-210: Default Warehouse/Location for auto-filling documents
    public int? DefaultWarehouseId { get; set; }
    public int? DefaultLocationId { get; set; }

    // R-040: Navigation property for Multi-UOM (R-029)
    public ICollection<ProductUom> ProductUoms { get; set; } = new List<ProductUom>();
}

