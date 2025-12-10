using System.Linq;
using InventoryERP.Application.Partners;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Partners;

/// <summary>
/// R-086: Partner (Cari) service implementation
/// </summary>
public class PartnerService : IPartnerService
{
    private readonly AppDbContext _db;
    
    public PartnerService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }
    
    public async Task<List<PartnerCrudListDto>> GetListAsync(PartnerType? filterByType = null, CancellationToken ct = default)
    {
        var query = _db.Partners.AsQueryable();
        
        if (filterByType.HasValue)
        {
            query = query.Where(p => p.PartnerType == filterByType.Value);
        }
        
        var list = await query
            .OrderBy(p => p.Name)
            .Select(p => new PartnerCrudListDto(
                p.Id,
                p.Name,
                p.PartnerType.ToString(),
                p.TaxId,
                p.NationalId,
                p.IsActive
            ))
            .ToListAsync(ct);
        
        return list;
    }
    
    public async Task<PartnerCrudDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var partner = await _db.Partners.FindAsync(new object[] { id }, ct);
        
        if (partner == null)
            return null;
        
        return new PartnerCrudDetailDto
        {
            Id = partner.Id,
            PartnerType = partner.PartnerType.ToString(),
            Name = partner.Name,
            TaxId = partner.TaxId,
            NationalId = partner.NationalId,
            Address = partner.Address,
            CreditLimitTry = partner.CreditLimitTry,
            PaymentTermDays = partner.PaymentTermDays,
            IsActive = partner.IsActive
        };
    }
    
    public async Task<int> SaveAsync(PartnerCrudDetailDto dto, CancellationToken ct = default)
    {
        // R-113: Normalize identifiers (remove non-digit characters)
        var normalizedTaxId = NormalizeIdentifier(dto.TaxId);
        var normalizedNationalId = NormalizeIdentifier(dto.NationalId);
        
        Partner partner;
        
        if (dto.Id == 0)
        {
            // R-113: Check for duplicates before creating new partner
            var duplicateCheck = _db.Partners
                .AsNoTracking()
                .Where(p => !p.IsDeleted && 
                           ((normalizedTaxId != null && p.TaxId == normalizedTaxId) || 
                            (normalizedNationalId != null && p.NationalId == normalizedNationalId)));
            
            var existingPartner = await duplicateCheck.FirstOrDefaultAsync(ct);
            
            if (existingPartner != null)
            {
                if (normalizedTaxId != null && existingPartner.TaxId == normalizedTaxId)
                {
                    throw new InvalidOperationException($"VKN '{normalizedTaxId}' zaten kayıtlı (Mevcut cari: {existingPartner.Name})");
                }
                if (normalizedNationalId != null && existingPartner.NationalId == normalizedNationalId)
                {
                    throw new InvalidOperationException($"TCKN '{normalizedNationalId}' zaten kayıtlı (Mevcut cari: {existingPartner.Name})");
                }
            }
            
            // Create new
            partner = new Partner
            {
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                Version = 1
            };
            _db.Partners.Add(partner);
        }
        else
        {
            // R-113: Check for duplicates excluding current partner ID
            var duplicateCheck = _db.Partners
                .AsNoTracking()
                .Where(p => !p.IsDeleted && 
                           p.Id != dto.Id &&
                           ((normalizedTaxId != null && p.TaxId == normalizedTaxId) || 
                            (normalizedNationalId != null && p.NationalId == normalizedNationalId)));
            
            var existingPartner = await duplicateCheck.FirstOrDefaultAsync(ct);
            
            if (existingPartner != null)
            {
                if (normalizedTaxId != null && existingPartner.TaxId == normalizedTaxId)
                {
                    throw new InvalidOperationException($"VKN '{normalizedTaxId}' zaten kayıtlı (Mevcut cari: {existingPartner.Name})");
                }
                if (normalizedNationalId != null && existingPartner.NationalId == normalizedNationalId)
                {
                    throw new InvalidOperationException($"TCKN '{normalizedNationalId}' zaten kayıtlı (Mevcut cari: {existingPartner.Name})");
                }
            }
            
            // Update existing
            partner = await _db.Partners.FindAsync(new object[] { dto.Id }, ct)
                ?? throw new InvalidOperationException($"Partner {dto.Id} not found");
            
            partner.ModifiedAt = DateTime.UtcNow;
            partner.Version++;
        }
        
        // Map DTO to entity
        if (!Enum.TryParse<PartnerType>(dto.PartnerType, out var partnerType))
        {
            throw new InvalidOperationException($"Invalid PartnerType: {dto.PartnerType}");
        }
        
        partner.PartnerType = partnerType;
        partner.Name = dto.Name;
        // R-113: Store normalized identifiers in canonical digit-only format
        partner.TaxId = normalizedTaxId;
        partner.NationalId = normalizedNationalId;
        partner.Address = dto.Address;
        partner.CreditLimitTry = dto.CreditLimitTry;
        partner.PaymentTermDays = dto.PaymentTermDays;
        partner.IsActive = dto.IsActive;
        
        // Validate business rules
        partner.Validate();
        
        await _db.SaveChangesAsync(ct);
        
        return partner.Id;
    }
    
    /// <summary>
    /// R-113: Normalize identifier by keeping only digits.
    /// Returns null if input is null/empty or contains no digits.
    /// </summary>
    private static string? NormalizeIdentifier(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;
        
        var digits = new string(input.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits) ? null : digits;
    }
    
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var partner = await _db.Partners.FindAsync(new object[] { id }, ct)
            ?? throw new InvalidOperationException($"Partner {id} not found");
        
        partner.IsDeleted = true;
        partner.ModifiedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync(ct);
    }
}
