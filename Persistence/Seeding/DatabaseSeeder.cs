using System;
using System.Collections.Generic;
using InventoryERP.Domain.Entities;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Persistence.Seeding;

public class DatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _sp;
    public DatabaseSeeder(IServiceProvider sp) => _sp = sp;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedAsync(db, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        // R-211: Run migrations first
        await db.Database.MigrateAsync(cancellationToken);
        
        // ---------------------------------------------------------
        // R-212 NUCLEAR FIX: FORCE SCHEMA PATCH (Self-Healing)
        // The AddProductDefaults migration is MISSING its Designer file.
        // We manually ensure columns exist to prevent crashes.
        // ---------------------------------------------------------
        Console.WriteLine(">>> [R-212] NUCLEAR FORCE SCHEMA PATCH STARTING...");
        
        // 1. Fix Product Table (try BOTH singular and plural naming)
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Product ADD COLUMN DefaultWarehouseId INTEGER DEFAULT NULL;"); Console.WriteLine(">>> APPLIED: Product.DefaultWarehouseId"); } catch { }
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Product ADD COLUMN DefaultLocationId INTEGER DEFAULT NULL;"); Console.WriteLine(">>> APPLIED: Product.DefaultLocationId"); } catch { }
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Product ADD COLUMN Cost TEXT DEFAULT '0';"); Console.WriteLine(">>> APPLIED: Product.Cost"); } catch { }
        
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN DefaultWarehouseId INTEGER DEFAULT NULL;"); Console.WriteLine(">>> APPLIED: Products.DefaultWarehouseId"); } catch { }
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN DefaultLocationId INTEGER DEFAULT NULL;"); Console.WriteLine(">>> APPLIED: Products.DefaultLocationId"); } catch { }
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN Cost TEXT DEFAULT '0';"); Console.WriteLine(">>> APPLIED: Products.Cost"); } catch { }
        
        // 2. Fix Document Table (Just in case)
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Document ADD COLUMN SourceWarehouseId INTEGER DEFAULT NULL;"); Console.WriteLine(">>> APPLIED: Document.SourceWarehouseId"); } catch { }
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Document ADD COLUMN DestinationWarehouseId INTEGER DEFAULT NULL;"); Console.WriteLine(">>> APPLIED: Document.DestinationWarehouseId"); } catch { }
        
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Documents ADD COLUMN SourceWarehouseId INTEGER DEFAULT NULL;"); Console.WriteLine(">>> APPLIED: Documents.SourceWarehouseId"); } catch { }
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Documents ADD COLUMN DestinationWarehouseId INTEGER DEFAULT NULL;"); Console.WriteLine(">>> APPLIED: Documents.DestinationWarehouseId"); } catch { }
        
        Console.WriteLine(">>> [R-212] NUCLEAR FORCE SCHEMA PATCH COMPLETE");
        // ---------------------------------------------------------

        if (!await db.Partners.AnyAsync(cancellationToken))
        {
            db.Partners.AddRange(
                new Partner { PartnerType = PartnerType.Customer, Name = "ACME" },
                new Partner { PartnerType = PartnerType.Supplier, Name = "SUPPLYCO" }
            );
        }

        if (!await db.Products.AnyAsync(cancellationToken))
        {
            db.Products.AddRange(
                new Product { Sku = "SKU-001", Name = "Ürün A", BaseUom = "Adet", VatRate = 20, Active = true },
                new Product { Sku = "SKU-002", Name = "Ürün B", BaseUom = "Adet", VatRate = 10, Active = true }
            );
        }

        await db.SaveChangesAsync(cancellationToken);

        // Optional: one initial stock in
        var p1 = await db.Products.FirstAsync(cancellationToken);
        if (!await db.StockMoves.AnyAsync(cancellationToken))
        {
            var doc = new Document
            {
                Type = DocumentType.PURCHASE_INVOICE,
                Number = "INIT-0001",
                Date = DateTime.UtcNow,
                Status = DocumentStatus.POSTED,
                Currency = "TRY",
                FxRate = 1m
            };
            var line = new DocumentLine
            {
                Document = doc,
                ItemId = p1.Id,
                Qty = 10m,
                Uom = "EA",
                UnitPrice = 100m,
                VatRate = 20
            };
            db.DocumentLines.Add(line);
            db.StockMoves.Add(new StockMove { ItemId = p1.Id, Date = DateTime.UtcNow, QtySigned = 10m, DocumentLine = line });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
