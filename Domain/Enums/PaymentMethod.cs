namespace InventoryERP.Domain.Enums;

/// <summary>
/// R-119: Payment method options for partners
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Cash payment (Nakit)
    /// </summary>
    Cash = 1,
    
    /// <summary>
    /// Credit card payment (Kredi Kartı)
    /// </summary>
    CreditCard = 2,
    
    /// <summary>
    /// Check payment (Çek)
    /// </summary>
    Check = 3
}
