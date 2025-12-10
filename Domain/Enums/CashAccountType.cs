namespace InventoryERP.Domain.Enums;

/// <summary>
/// R-007: Cash & Bank account types supported by the finance module.
/// </summary>
public enum CashAccountType
{
    /// <summary>
    /// Physical cash register (Kasa)
    /// </summary>
    Cash = 1,

    /// <summary>
    /// Bank account (Banka)
    /// </summary>
    Bank = 2
}
