namespace InventoryERP.Domain.Enums;

/// <summary>
/// R-086/R-119: Partner/Cari type classification
/// </summary>
public enum PartnerType
{
    /// <summary>
    /// Customer (Müşteri)
    /// </summary>
    Customer = 1,
    
    /// <summary>
    /// Supplier (Tedarikçi/Satıcı)
    /// </summary>
    Supplier = 2,
    
    /// <summary>
    /// Both Customer and Supplier (Hem Müşteri Hem Satıcı)
    /// </summary>
    Both = 3,
    
    /// <summary>
    /// Other (Diğer)
    /// </summary>
    Other = 4
}
