using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class DocumentLine : EntityBase
{
    public int DocumentId { get; set; }
    public int ItemId { get; set; }
    public decimal Qty { get; set; }
    public string Uom { get; set; } = null!;
    // Coefficient to convert this line's UOM to product base UOM (e.g., KOLI -> 12)
    public decimal Coefficient { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public int VatRate { get; set; }
    // R-237: Discount amount for invoice lines
    public decimal DiscountAmount { get; set; }
    public int? LotId { get; set; }
    public Lot? Lot { get; set; }
    public int? ProductVariantId { get; set; }
    public int? SourceLocationId { get; set; }
    public int? DestinationLocationId { get; set; }

    public Document? Document { get; set; }
    public Product? Item { get; set; }
    public ICollection<StockMove> StockMoves { get; set; } = new List<StockMove>();
}

