using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryERP.Application.Products;

/// <summary>
/// Service for managing product price lists.
/// PRD Reference: prices(item_id, liste_kod, uom, fiyat, doviz, başlangıç, bitiş)
/// Use Case: Support multiple price lists (NAKİT, VADELİ, BAYİ, etc.) with date-based validity.
/// </summary>
public interface IPriceListService
{
    /// <summary>
    /// Gets all price list entries for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>List of all prices for the product, regardless of validity dates.</returns>
    Task<List<PriceDto>> GetPricesByProductIdAsync(int productId);

    /// <summary>
    /// Adds a new price list entry.
    /// </summary>
    /// <param name="dto">Price data to create.</param>
    /// <returns>The created price entry.</returns>
    /// <exception cref="ArgumentException">If validation fails (e.g., product doesn't exist).</exception>
    Task<PriceDto> AddPriceAsync(CreatePriceDto dto);

    /// <summary>
    /// Updates an existing price list entry.
    /// </summary>
    /// <param name="priceId">The price ID to update.</param>
    /// <param name="dto">Updated price data.</param>
    /// <returns>The updated price entry.</returns>
    /// <exception cref="KeyNotFoundException">If price doesn't exist.</exception>
    Task<PriceDto> UpdatePriceAsync(int priceId, UpdatePriceDto dto);

    /// <summary>
    /// Deletes a price list entry.
    /// </summary>
    /// <param name="priceId">The price ID to delete.</param>
    /// <exception cref="KeyNotFoundException">If price doesn't exist.</exception>
    Task DeletePriceAsync(int priceId);

    /// <summary>
    /// Gets the effective price for a product at a specific date.
    /// Filters by list code, UOM, and date range.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="listCode">Price list code (e.g., NAKİT, VADELİ).</param>
    /// <param name="uomName">Unit of measure.</param>
    /// <param name="date">The date to check (defaults to today).</param>
    /// <returns>The effective price, or null if none found.</returns>
    Task<PriceDto?> GetEffectivePriceAsync(int productId, string listCode, string uomName, DateTime? date = null);
}
