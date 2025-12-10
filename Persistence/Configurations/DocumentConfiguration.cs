using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("Document");
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Number).IsRequired(false).HasMaxLength(32);
        b.Property(x => x.Date).IsRequired();
        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        b.Property(x => x.FxRate).HasPrecision(18, 6);
        b.Property(x => x.ExternalId).HasMaxLength(64);
        b.Property(x => x.DueDate);

        b.HasOne(x => x.Partner)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CashAccount)
            .WithMany()
            .HasForeignKey(x => x.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.Type, x.Date });
        b.HasIndex(x => new { x.Type, x.Number })
            .IsUnique()
            .HasFilter("\"Number\" IS NOT NULL");
        b.HasIndex(x => x.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");

        b.ToTable(t => t.HasCheckConstraint(
            "CK_Document_FxRate",
            "CASE WHEN Currency <> 'TRY' THEN (FxRate + 0.0) > 0.0 ELSE 1 END"));

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
