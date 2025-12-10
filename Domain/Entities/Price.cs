using System;

namespace InventoryERP.Domain.Entities;

/// <summary>
/// Represents a price list entry for a product.
/// PRD Reference: prices(item_id, liste_kod, uom, fiyat, doviz, başlangıç, bitiş)
/// Use Case: Multiple price lists (NAKİT, VADELİ, BAYİ, etc.) with different currencies and validity periods.
/// </summary>
public class Price
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Product (item_id in PRD).
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Price list code (liste_kod in PRD).
    /// Examples: NAKİT, VADELİ, BAYİ, EXPORT, WHOLESALE
    /// </summary>
    public string ListCode { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measure for this price (uom in PRD).
    /// Must match one of the product's UOMs (base or alternative from ProductUom table).
    /// Examples: EA, KG, KOLI, PALET
    /// </summary>
    public string UomName { get; set; } = string.Empty;

    /// <summary>
    /// Unit price in the specified currency (fiyat in PRD).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Currency code (doviz in PRD).
    /// Examples: TRY, USD, EUR, GBP
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Start date of validity period (başlangıç in PRD).
    /// Null means valid from the beginning of time.
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// End date of validity period (bitiş in PRD).
    /// Null means valid forever.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to Product.
    /// </summary>
    public Product Product { get; set; } = null!;
}
