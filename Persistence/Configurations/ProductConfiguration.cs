using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Product");
        b.HasKey(x => x.Id);
        b.Property(x => x.Sku).IsRequired().HasMaxLength(64).HasColumnType("TEXT COLLATE NOCASE");
        b.Property(x => x.Name).IsRequired().HasMaxLength(256);
        b.Property(x => x.BaseUom).IsRequired().HasMaxLength(16);
        b.Property(x => x.ReservedQty).HasPrecision(18,3).HasDefaultValue(0m);
        b.Property(x => x.Cost).HasPrecision(18,6).HasDefaultValue(0m);
        b.Property(x => x.Active).HasDefaultValue(true);
        b.Property(x => x.VatRate).IsRequired();
        b.Property(x => x.Barcode)
            .HasMaxLength(13)
            .HasColumnType("TEXT");

        b.HasIndex(x => x.Sku).IsUnique();
        b.HasIndex(x => x.Barcode).IsUnique();

        // R-350: REMOVED - Allow dynamic VAT rates (0-100). C# validation now handles range.
        // Previous constraint was: VatRate IN (1,10,20)
        // b.ToTable(t => t.HasCheckConstraint(
        //     "CK_Product_VatRate",
        //     "VatRate IN (1,10,20)"));

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
