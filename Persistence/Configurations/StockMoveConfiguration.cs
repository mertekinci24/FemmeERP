using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class StockMoveConfiguration : IEntityTypeConfiguration<StockMove>
{
    public void Configure(EntityTypeBuilder<StockMove> b)
    {
        b.ToTable("StockMove");
        b.HasKey(x => x.Id);

        b.Property(x => x.QtySigned).HasPrecision(18, 3);
        b.Property(x => x.UnitCost).HasPrecision(18, 6);

        b.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.DocumentLine)
            .WithMany(x => x.StockMoves)
            .HasForeignKey(x => x.DocLineId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        b.HasIndex(x => new { x.ItemId, x.Date });

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

