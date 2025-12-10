using InventoryERP.Domain.Common;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Domain.Entities;

public class Document : EntityBase
{
    public DocumentType Type { get; set; }
    public string? Number { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime Date { get; set; }
    public DocumentStatus Status { get; set; }
    public int? PartnerId { get; set; }
    public int? CashAccountId { get; set; }
    public string Currency { get; set; } = "TRY";
    public int? SourceWarehouseId { get; set; }
    public int? DestinationWarehouseId { get; set; }
    public decimal? FxRate { get; set; }
    public string? ExternalId { get; set; }
    public string? Description { get; set; } // R-249: Invoice notes/description

    public Partner? Partner { get; set; }
    public CashAccount? CashAccount { get; set; }
    public ICollection<DocumentLine> Lines { get; set; } = new List<DocumentLine>();
    public decimal TotalTry { get; set; } // Total amount in TRY for cash posting
}

