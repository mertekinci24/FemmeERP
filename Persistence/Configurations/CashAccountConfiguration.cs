using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class CashAccountConfiguration : IEntityTypeConfiguration<CashAccount>
{
    public void Configure(EntityTypeBuilder<CashAccount> b)
    {
        b.ToTable("CashAccount");
        b.HasKey(x => x.Id);

        b.HasQueryFilter(x => !x.IsDeleted);

        b.Property(x => x.Name).HasMaxLength(128);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.BankName).HasMaxLength(128);
        b.Property(x => x.BankBranch).HasMaxLength(128);
        b.Property(x => x.AccountNumber).HasMaxLength(64);
        b.Property(x => x.Iban).HasMaxLength(34);
        b.Property(x => x.SwiftCode).HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(512);
    }
}
