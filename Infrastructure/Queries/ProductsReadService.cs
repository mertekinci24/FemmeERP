using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Products;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Queries;

public sealed class ProductsReadService : IProductsReadService
{
    private readonly AppDbContext _db;
    public ProductsReadService(AppDbContext db) => _db = db;

    // R-282: Added includePassive parameter
    public async Task<System.Collections.Generic.IReadOnlyList<ProductRowDto>> GetListAsync(string? search, int? warehouseId = null, bool includePassive = false)
    {
        var q = _db.Products.AsNoTracking();
        
        // R-282: Default filter - Active Only
        if (!includePassive)
        {
            q = q.Where(p => p.Active);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            // R-277: Case Insensitive Search + Enterprise Fields (Category, Brand)
            var s = search.ToLower();
            q = q.Where(p => p.Sku.ToLower().Contains(s) || 
                             p.Name.ToLower().Contains(s) ||
                             (p.Category != null && p.Category.ToLower().Contains(s)) || 
                             (p.Brand != null && p.Brand.ToLower().Contains(s)));
        }

        var products = await q
            .OrderBy(p => p.Sku)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();
        
        var moveQuery = _db.StockMoves.Where(m => productIds.Contains(m.ItemId));

        // R-210: Filter by Warehouse
        if (warehouseId.HasValue)
        {
            var locationIds = await _db.Locations
                .Where(l => l.WarehouseId == warehouseId.Value)
                .Select(l => l.Id)
                .ToListAsync();
            
            // If warehouse has no locations, stock is 0
            if (!locationIds.Any())
            {
                 // Filter out products that have never been in this warehouse (no moves) AND have 0 stock
                 // But since we are filtering the LIST, we should probably only show products that have SOME relation to this warehouse?
                 // Requirement: "Only show products that have a non-zero balance OR have had movement in that warehouse."
                 // If no locations, then no movements possible in this warehouse. So return empty list?
                 // Or return all products with 0 stock?
                 // "Issue 1 (Filter): When filtering Stock List by a specific Warehouse, it shows ALL products (even those with 0 history in that warehouse)."
                 // So we should return empty list if no locations exist in that warehouse.
                 return new List<ProductRowDto>();
            }

            // Filter moves relevant to this warehouse
            moveQuery = moveQuery.Where(m => 
                (m.SourceLocationId.HasValue && locationIds.Contains(m.SourceLocationId.Value)) || 
                (m.DestinationLocationId.HasValue && locationIds.Contains(m.DestinationLocationId.Value)));
        }

        var stockMoves = await moveQuery
            .Select(m => new { m.ItemId, m.QtySigned })
            .ToListAsync();

        var stockLookup = stockMoves
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QtySigned));

        // R-210: Apply the filter to the product list
        // If warehouseId is set, only include products that are in the stockLookup (meaning they have moves in that warehouse)
        // OR if the user wants to see all products but with 0 stock?
        // Requirement: "Only show products that have a non-zero balance OR have had movement in that warehouse."
        // stockLookup contains items with moves. So if warehouseId is set, we filter by stockLookup keys.
        
        IEnumerable<InventoryERP.Domain.Entities.Product> filteredProducts = products;
        if (warehouseId.HasValue)
        {
            filteredProducts = products.Where(p => stockLookup.ContainsKey(p.Id));
        }

        // R-274: Enterprise Grid - Calculate AvailableQty and include all metrics
        return filteredProducts
            .Select(p => {
                var onHand = stockLookup.TryGetValue(p.Id, out var qty) ? qty : 0m;
                var available = onHand - p.ReservedQty; // Available = OnHand - Reserved
                return new ProductRowDto(
                    p.Id,
                    p.Sku,
                    p.Name,
                    p.BaseUom,
                    p.VatRate,
                    p.Active,
                    onHand,
                    p.ReservedQty,   // R-274: Reserved quantity
                    available,       // R-274: OnHand - Reserved
                    p.Cost,          // R-274: Purchase price
                    p.SalesPrice,    // Sales price
                    p.Barcode,       // R-274: Barcode
                    p.Category,      // R-274: Category
                    p.Brand,         // R-274: Brand
                    p.DefaultWarehouseId,
                    p.DefaultLocationId);
            })
            .ToList();
    }

    public async Task<System.Collections.Generic.IReadOnlyList<InventoryERP.Application.Products.ProductUomDto>> GetUomsAsync(int productId)
    {
        return await _db.ProductUoms
            .Where(u => u.ProductId == productId && !u.IsDeleted)
            .OrderBy(u => u.UomName)
            .Select(u => new InventoryERP.Application.Products.ProductUomDto(u.UomName, u.Coefficient))
            .ToListAsync();
    }

    public async Task<System.Collections.Generic.IReadOnlyList<InventoryERP.Application.Products.ProductLotDto>> GetLotsForProductAsync(int productId)
    {
        return await _db.Lots
            .Where(l => l.ProductId == productId && !l.IsDeleted)
            .OrderBy(l => l.LotNumber)
            .Select(l => new InventoryERP.Application.Products.ProductLotDto(l.Id, l.LotNumber, l.ExpiryDate))
            .ToListAsync();
    }

    public async Task<System.Collections.Generic.IReadOnlyList<InventoryERP.Application.Products.ProductVariantDto>> GetVariantsAsync(int productId)
    {
        return await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Code)
            .Select(v => new InventoryERP.Application.Products.ProductVariantDto(v.Id, v.Code))
            .ToListAsync();
    }

    public async Task<ProductRowDto?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Sku.ToLower() == code.ToLower());
        if (p == null) return null;
        var onHandRows = await _db.StockMoves
            .Where(m => m.ItemId == p.Id)
            .Select(m => m.QtySigned)
            .ToListAsync();
        var onHand = onHandRows.Sum();
        // R-274: Enterprise Grid - Full metrics for barcode lookup
        var available = onHand - p.ReservedQty;
        return new ProductRowDto(
            p.Id, p.Sku, p.Name, p.BaseUom, p.VatRate, p.Active, 
            onHand, p.ReservedQty, available, p.Cost, p.SalesPrice, 
            p.Barcode, p.Category, p.Brand, 
            p.DefaultWarehouseId, p.DefaultLocationId);
    }
    // R-282: Fetch specific items by ID to resolve ghost references in historical documents
    public async Task<System.Collections.Generic.IReadOnlyList<ProductRowDto>> GetByIdsAsync(System.Collections.Generic.IEnumerable<int> ids)
    {
        var distinctIds = ids.Distinct().ToList();
        if (!distinctIds.Any()) return new List<ProductRowDto>();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => distinctIds.Contains(p.Id))
            .ToListAsync();

        var stockMoves = await _db.StockMoves
            .Where(m => distinctIds.Contains(m.ItemId))
            .Select(m => new { m.ItemId, m.QtySigned })
            .ToListAsync();

        var stockLookup = stockMoves
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QtySigned));

        return products.Select(p => {
             var onHand = stockLookup.TryGetValue(p.Id, out var qty) ? qty : 0m;
             var available = onHand - p.ReservedQty;
             return new ProductRowDto(
                p.Id, p.Sku, p.Name, p.BaseUom, p.VatRate, p.Active, 
                onHand, p.ReservedQty, available, p.Cost, p.SalesPrice, 
                p.Barcode, p.Category, p.Brand, 
                p.DefaultWarehouseId, p.DefaultLocationId);
        }).ToList();
    }

    public async Task<System.Collections.Generic.IReadOnlyList<InventoryERP.Application.Products.WarehouseDto>> GetWarehousesAsync()
    {
        return await _db.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new InventoryERP.Application.Products.WarehouseDto(w.Id, w.Name, w.IsDefault))
            .ToListAsync();
    }
}
