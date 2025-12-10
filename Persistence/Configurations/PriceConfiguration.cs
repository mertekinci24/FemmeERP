using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

/// <summary>
/// EF Core configuration for Price entity.
/// PRD Reference: prices(item_id, liste_kod, uom, fiyat, doviz, başlangıç, bitiş)
/// </summary>
public class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> builder)
    {
        builder.ToTable("Prices");

        // Primary key
        builder.HasKey(p => p.Id);

        // ProductId is required (FK to Products)
        builder.Property(p => p.ProductId)
            .IsRequired();

        // ListCode is required (e.g., NAKİT, VADELİ, BAYİ)
        builder.Property(p => p.ListCode)
            .IsRequired()
            .HasMaxLength(50);

        // UomName is required (must match Product's base or alternative UOM)
        builder.Property(p => p.UomName)
            .IsRequired()
            .HasMaxLength(20);

        // UnitPrice with precision (18,4) for monetary values
        builder.Property(p => p.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 4);

        // Currency is required (TRY, USD, EUR, etc.)
        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("TRY");

        // ValidFrom is optional (null means valid from beginning)
        builder.Property(p => p.ValidFrom)
            .IsRequired(false);

        // ValidTo is optional (null means valid forever)
        builder.Property(p => p.ValidTo)
            .IsRequired(false);

        // Foreign key relationship
        builder.HasOne(p => p.Product)
            .WithMany() // No navigation property in Product yet (can add later if needed)
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade); // Delete prices when product is deleted

        // Composite index for efficient lookups
        // Common query: "Get all prices for product X with list code Y in UOM Z valid on date D"
        builder.HasIndex(p => new { p.ProductId, p.ListCode, p.UomName, p.ValidFrom, p.ValidTo })
            .HasDatabaseName("IX_Prices_ProductId_ListCode_UomName_ValidDates");

        // Additional index for product-level queries (Get all prices for a product)
        builder.HasIndex(p => p.ProductId)
            .HasDatabaseName("IX_Prices_ProductId");
    }
}
