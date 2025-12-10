using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class PartnerLedgerEntryConfiguration : IEntityTypeConfiguration<PartnerLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PartnerLedgerEntry> b)
    {
        b.ToTable("PartnerLedgerEntry");
        b.HasKey(x => x.Id);

        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        b.Property(x => x.FxRate).HasPrecision(18, 6);
        b.Property(x => x.Debit).HasPrecision(18, 2);
        b.Property(x => x.Credit).HasPrecision(18, 2);
        b.Property(x => x.AmountTry).HasPrecision(18, 2);

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.DocType).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.DocNumber).HasMaxLength(64);

        b.HasOne(x => x.Partner)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

    b.HasIndex(x => new { x.PartnerId, x.Date });
    b.HasIndex(x => new { x.PartnerId, x.Status, x.DueDate }); // R-006: aging index
        b.HasIndex(x => x.Status);

        b.ToTable(t => t.HasCheckConstraint(
            "CK_PartnerLedger_DebitCreditXor",
            "(((Debit + 0.0) = 0.0 AND (Credit + 0.0) <> 0.0) OR ((Debit + 0.0) <> 0.0 AND (Credit + 0.0) = 0.0))"));

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
