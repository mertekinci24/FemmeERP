namespace InventoryERP.Application.Products;

// R-274: Enterprise Product Grid DTO with full metrics
public sealed record ProductRowDto(
    int Id, 
    string Sku, 
    string Name, 
    string BaseUom, 
    int VatRate,
    bool Active, 
    decimal OnHandQty, 
    decimal ReservedQty,      // R-274: Reserved quantity
    decimal AvailableQty,     // R-274: OnHand - Reserved
    decimal Cost,             // R-274: Purchase price (Alış Fiyatı)
    decimal SalesPrice,       // Sales price (Satış Fiyatı)
    string? Barcode,          // R-274: Product barcode
    string? Category,         // R-274: Product category
    string? Brand,            // R-274: Product brand (Marka)
    int? DefaultWarehouseId = null, 
    int? DefaultLocationId = null);

public sealed record ProductUomDto(string UomName, decimal Coefficient);

public sealed record ProductLotDto(int Id, string LotNumber, System.DateTime? ExpiryDate);
public sealed record ProductVariantDto(int Id, string Code);
public sealed record WarehouseDto(int Id, string Name, bool IsDefault);

public interface IProductsReadService
{
// R-282: Updated signature for Passive Lifecycle Management
    System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ProductRowDto>> GetListAsync(string? search, int? warehouseId = null, bool includePassive = false);
    
    // R-282: Fetch specific items (even passive ones) to prevent Ghost Products
    System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ProductRowDto>> GetByIdsAsync(System.Collections.Generic.IEnumerable<int> ids);
    
    System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId);
    System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId);
    System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId);
    System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<WarehouseDto>> GetWarehousesAsync();
    System.Threading.Tasks.Task<ProductRowDto?> GetByCodeAsync(string code);
}


