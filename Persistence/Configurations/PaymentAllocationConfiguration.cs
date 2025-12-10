using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> b)
    {
        b.ToTable("PaymentAllocation");
        b.HasKey(x => x.Id);
        b.Property(x => x.AmountTry).HasPrecision(18, 2);

        b.HasOne(x => x.PaymentEntry)
            .WithMany()
            .HasForeignKey(x => x.PaymentEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.InvoiceEntry)
            .WithMany()
            .HasForeignKey(x => x.InvoiceEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.InvoiceEntryId);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

