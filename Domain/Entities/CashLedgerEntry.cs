using InventoryERP.Domain.Common;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Domain.Entities;

/// <summary>
/// R-131: Cash ledger entry for tracking cash/bank movements.
/// </summary>
public class CashLedgerEntry : EntityBase
{
    public int CashAccountId { get; set; }
    public int? DocId { get; set; }
    public DocumentType? DocType { get; set; }
    public string? DocNumber { get; set; }

    public DateTime Date { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal FxRate { get; set; } = 1.0m;

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public decimal AmountTry { get; set; }
    
    public string? Description { get; set; }
    public LedgerStatus Status { get; set; } = LedgerStatus.OPEN;

    public CashAccount? CashAccount { get; set; }
    public Document? Document { get; set; }
}
