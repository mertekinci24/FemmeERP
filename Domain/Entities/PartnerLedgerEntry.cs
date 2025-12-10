using InventoryERP.Domain.Common;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Domain.Entities;

public class PartnerLedgerEntry : EntityBase
{
    public int PartnerId { get; set; }
    public int? DocId { get; set; }
    public DocumentType? DocType { get; set; }
    public string? DocNumber { get; set; }

    public DateTime Date { get; set; }
    public DateTime? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal FxRate { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal AmountTry { get; set; }
    public string? Description { get; set; }
    public LedgerStatus Status { get; set; }
    // Index: (PartnerId, Status, Date)

    public Partner? Partner { get; set; }
    public Document? Document { get; set; }
}
