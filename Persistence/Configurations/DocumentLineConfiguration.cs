using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class DocumentLineConfiguration : IEntityTypeConfiguration<DocumentLine>
{
    public void Configure(EntityTypeBuilder<DocumentLine> b)
    {
        b.ToTable("DocumentLine");
        b.HasKey(x => x.Id);

        b.Property(x => x.Qty).HasPrecision(18, 3);
        b.Property(x => x.UnitPrice).HasPrecision(18, 6);
        b.Property(x => x.Uom).IsRequired().HasMaxLength(16);
        b.Property(x => x.VatRate).IsRequired();

        b.HasOne(x => x.Document)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.ToTable(t => t.HasCheckConstraint(
            "CK_DocumentLine_VatRate",
            "VatRate IN (1,10,20)"));

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
