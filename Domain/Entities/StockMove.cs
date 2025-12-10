using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class StockMove : EntityBase
{
    public int ItemId { get; set; }
    public DateTime Date { get; set; }
    public decimal QtySigned { get; set; }
    public decimal? UnitCost { get; set; }
    public int? DocLineId { get; set; }
    public string? Note { get; set; }
    public int? ProductVariantId { get; set; }
    public int? SourceLocationId { get; set; }
    public int? DestinationLocationId { get; set; }

    public Product? Item { get; set; }
    public DocumentLine? DocumentLine { get; set; }
}

