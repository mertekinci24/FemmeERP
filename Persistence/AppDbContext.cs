using Microsoft.EntityFrameworkCore;
using System.Reflection;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Common;
using InventoryERP.Domain.Services;

namespace Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> opt) : DbContext(opt)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMove> StockMoves => Set<StockMove>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentLine> DocumentLines => Set<DocumentLine>();
    public DbSet<ProductUom> ProductUoms => Set<ProductUom>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<PartnerLedgerEntry> PartnerLedgerEntries => Set<PartnerLedgerEntry>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<BomItem> BomItems => Set<BomItem>();
    public DbSet<Price> Prices => Set<Price>(); // R-041: Price lists (PRD: prices table)
    public DbSet<CashAccount> CashAccounts => Set<CashAccount>();
    public DbSet<CashLedgerEntry> CashLedgerEntries => Set<CashLedgerEntry>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        mb.Entity<DocumentSequence>()
            .HasIndex(s => new { s.DocumentType, s.Year })
            .IsUnique();

        foreach (var et in mb.Model.GetEntityTypes())
        {
            if (typeof(EntityBase).IsAssignableFrom(et.ClrType))
            {
                mb.Entity(et.ClrType).Property<DateTime?>(nameof(EntityBase.ModifiedAt)).IsConcurrencyToken();
                mb.Entity(et.ClrType).Property<int>(nameof(EntityBase.Version)).IsConcurrencyToken();
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.ModifiedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = utcNow;
                entry.Entity.Version += 1;
            }

            if (entry.Entity.IsDeleted && entry.Entity.DeletedAt is null)
            {
                entry.Entity.DeletedAt = utcNow;
            }
            if (!entry.Entity.IsDeleted && entry.Entity.DeletedAt is not null)
            {
                entry.Entity.DeletedAt = null;
            }
        }

        // SQLite ölçek zorlamadığı için tutarlı yuvarlama uygula
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
                static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
                static decimal R6(decimal v) => Math.Round(v, 6, MidpointRounding.AwayFromZero);

                switch (entry.Entity)
                {
                    case DocumentLine dl:
                        dl.Qty = R3(dl.Qty);
                        dl.UnitPrice = R6(dl.UnitPrice);
                        break;
                    case StockMove sm:
                        sm.QtySigned = R3(sm.QtySigned);
                        if (sm.UnitCost.HasValue) sm.UnitCost = R6(sm.UnitCost.Value);
                        break;
                    case PartnerLedgerEntry ple:
                        ple.Debit = R2(ple.Debit);
                        ple.Credit = R2(ple.Credit);
                        ple.AmountTry = R2(ple.AmountTry);
                        ple.FxRate = R6(ple.FxRate);
                        break;
                    case CashLedgerEntry cle:
                        cle.Debit = R2(cle.Debit);
                        cle.Credit = R2(cle.Credit);
                        cle.Balance = R2(cle.Balance);
                        cle.AmountTry = R2(cle.AmountTry);
                        cle.FxRate = R6(cle.FxRate);
                        break;
                    case PaymentAllocation pa:
                        pa.AmountTry = R2(pa.AmountTry);
                        break;
                    case Document d:
                        if (d.FxRate.HasValue) d.FxRate = R6(d.FxRate.Value);
                        break;
                }
            }
        }

            // Compute and persist document totals (Net, Vat, Gross) and TotalTry centrally
            // for any Document instances that were added or modified, or whose lines changed.
            var docsToRecalculate = new HashSet<Document>();

            // Documents directly added/modified
            foreach (var de in ChangeTracker.Entries<Document>().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                docsToRecalculate.Add(de.Entity);
            }

            // Documents affected by line changes
            foreach (var dle in ChangeTracker.Entries<DocumentLine>().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            {
                var doc = dle.Entity.Document;
                if (doc is not null)
                {
                    docsToRecalculate.Add(doc);
                }
                else if (dle.Entity.DocumentId > 0)
                {
                    // try to find tracked Document first
                    var tracked = ChangeTracker.Entries<Document>().FirstOrDefault(x => x.Entity.Id == dle.Entity.DocumentId)?.Entity;
                    if (tracked is not null) docsToRecalculate.Add(tracked);
                    else
                    {
                        // load from database with lines (safe: this is before SaveChanges)
                        var loaded = Documents.Include(d => d.Lines).FirstOrDefault(d => d.Id == dle.Entity.DocumentId);
                        if (loaded is not null) docsToRecalculate.Add(loaded);
                    }
                }
            }

            foreach (var doc in docsToRecalculate)
            {
                // Only compute totals when document has explicit lines. If no lines are present
                // we assume the caller set Document.TotalTry explicitly (e.g., cash receipts)
                // and we should not overwrite it.
                var lines = doc.Lines;
                if (lines == null || !lines.Any())
                {
                    // try to load lines if the document is tracked with an Id
                    if (doc.Id > 0)
                    {
                        var loaded = Documents.Include(d => d.Lines).FirstOrDefault(d => d.Id == doc.Id);
                        if (loaded is not null) lines = loaded.Lines;
                    }
                }

                if (lines != null && lines.Any())
                {
                    var totals = DocumentCalculator.ComputeTotals(lines);
                    var fx = doc.FxRate ?? 1m;
                    doc.TotalTry = Math.Round(totals.Gross * fx, 2, MidpointRounding.AwayFromZero);
                }
            }
    }
}
