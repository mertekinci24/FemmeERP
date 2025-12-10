using InventoryERP.Domain.Common;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Domain.Entities;

/// <summary>
/// R-007: Cash & bank account master data.
/// </summary>
public class CashAccount : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public CashAccountType Type { get; set; } = CashAccountType.Cash;
    public string Currency { get; set; } = "TRY";
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? SwiftCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CashLedgerEntry> LedgerEntries { get; set; } = new List<CashLedgerEntry>();
}
