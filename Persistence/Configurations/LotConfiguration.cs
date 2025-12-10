using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> b)
    {
        b.ToTable("Lot");

        b.HasKey(x => x.Id);

        b.Property(x => x.LotNumber)
            .IsRequired()
            .HasMaxLength(128);

        // Enforce unique lot numbers per product
        b.HasIndex(x => new { x.ProductId, x.LotNumber })
            .IsUnique();

        // Optional: filter out soft-deleted lots if using soft delete
        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
