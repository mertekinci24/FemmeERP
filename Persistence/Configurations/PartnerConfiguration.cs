using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

/// <summary>
/// R-086: Partner entity configuration with enhanced fields
/// </summary>
public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> b)
    {
        b.ToTable("Partner");
        b.HasKey(x => x.Id);
        
        // R-086: Core fields
    b.Property(x => x.Name).IsRequired().HasMaxLength(256);
    b.Property(x => x.TaxId).HasMaxLength(10); // VKN: 10 digits
    b.Property(x => x.NationalId).HasMaxLength(11); // TCKN: 11 digits
    b.Property(x => x.Address).HasMaxLength(500);
    b.Property(x => x.Email).HasMaxLength(128);
    b.Property(x => x.Phone).HasMaxLength(32);
        
    // Financial fields
    b.Property(x => x.CreditLimitTry).HasPrecision(18, 2);
    b.Property(x => x.PaymentTermDays);
        b.Property(x => x.IsActive).IsRequired();
        
        // R-086: PartnerType enum
        b.Property(x => x.PartnerType)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        
        // Legacy compatibility - Title maps to Name
        b.Ignore(x => x.Title);
        b.Ignore(x => x.TaxNo);
        b.Ignore(x => x.Role);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

