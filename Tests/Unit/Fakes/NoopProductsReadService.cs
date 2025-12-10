using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryERP.Application.Products;

namespace InventoryERP.Application.Products.Fakes;

public sealed class NoopProductsReadService : IProductsReadService
{
    public Task<IReadOnlyList<ProductRowDto>> GetListAsync(string? search)
        => Task.FromResult((IReadOnlyList<ProductRowDto>)new List<ProductRowDto>());
    public Task<IReadOnlyList<ProductUomDto>> GetUomsAsync(int productId)
        => Task.FromResult((IReadOnlyList<ProductUomDto>)new List<ProductUomDto>());
    public Task<IReadOnlyList<ProductLotDto>> GetLotsForProductAsync(int productId)
        => Task.FromResult((IReadOnlyList<ProductLotDto>)new List<ProductLotDto>());
    public Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(int productId)
        => Task.FromResult((IReadOnlyList<ProductVariantDto>)new List<ProductVariantDto>());
    public Task<ProductRowDto?> GetByCodeAsync(string code) => Task.FromResult<ProductRowDto?>(null);
}
