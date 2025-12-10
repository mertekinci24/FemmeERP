using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

/// <summary>
/// R-131: Cash ledger entry EF Core configuration.
/// </summary>
public class CashLedgerEntryConfiguration : IEntityTypeConfiguration<CashLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CashLedgerEntry> builder)
    {
        builder.ToTable("CashLedgerEntries");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("TRY");
        
        builder.Property(e => e.FxRate)
            .HasPrecision(18, 6)
            .HasDefaultValue(1.0m);
        
        builder.Property(e => e.Debit)
            .HasPrecision(18, 2);
        
        builder.Property(e => e.Credit)
            .HasPrecision(18, 2);
        
        builder.Property(e => e.Balance)
            .HasPrecision(18, 2);
        
        builder.Property(e => e.AmountTry)
            .HasPrecision(18, 2);
        
        builder.Property(e => e.Description)
            .HasMaxLength(500);
        
        builder.HasIndex(e => new { e.CashAccountId, e.Date });
        builder.HasIndex(e => new { e.CashAccountId, e.Status });
        
        builder.HasOne(e => e.CashAccount)
            .WithMany(c => c.LedgerEntries)
            .HasForeignKey(e => e.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
