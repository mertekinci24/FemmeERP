using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Application.Partners;

/// <summary>
/// R-086: Partner (Cari) service interface for CRUD operations
/// </summary>
public interface IPartnerService
{
    /// <summary>
    /// Get all partners (optionally filtered by type)
    /// </summary>
    Task<List<PartnerCrudListDto>> GetListAsync(PartnerType? filterByType = null, CancellationToken ct = default);
    
    /// <summary>
    /// Get partner detail by ID
    /// </summary>
    Task<PartnerCrudDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    
    /// <summary>
    /// Create or update partner
    /// </summary>
    Task<int> SaveAsync(PartnerCrudDetailDto dto, CancellationToken ct = default);
    
    /// <summary>
    /// Delete partner (soft delete)
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
