using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Xunit;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using InventoryERP.Domain.Entities;

namespace Tests.Integration;

public class BackupCreatesZip_WithDb
{
    [Fact(Timeout = 60000)]
    public async Task Backup_creates_zip_and_contains_inventory_db()
    {
        var root = Path.Combine(Path.GetTempPath(), "InventoryERP_Test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        try
        {
            var basePath = Path.Combine(root, "appdata");
            Directory.CreateDirectory(basePath);
            var dbPath = Path.Combine(basePath, "inventory.db");

            // create a file-based sqlite db and apply migrations + seed a product
            var connStr = $"Data Source={dbPath}";
            var opt = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connStr)
                .Options;
            using (var ctx = new AppDbContext(opt))
            {
                // Ensure clean file and create schema from current model
                try { ctx.Database.EnsureDeleted(); } catch { }
                ctx.Database.EnsureCreated();
                ctx.Products.Add(new Product { Name = "TestProd", Sku = "TP-1", BaseUom = "pcs", VatRate = 1, Active = true });
                await ctx.SaveChangesAsync();
            }

            // ensure file handles are released before zipping
            GC.Collect(); GC.WaitForPendingFinalizers();
            await Task.Delay(50);

            var validator = new BackupValidator();
            var svc = new BackupService(validator, basePath);

            var outDir = Path.Combine(root, "out");
            Directory.CreateDirectory(outDir);

            var zip = await svc.BackupAsync(outDir);
            Assert.True(File.Exists(zip), "Backup zip should exist");
            var fi = new FileInfo(zip);
            Assert.True(fi.Length > 0, "Backup zip should not be empty");

            using var za = ZipFile.OpenRead(zip);
            var entry = za.GetEntry("inventory.db");
            Assert.NotNull(entry);
            Assert.True(entry.Length > 0, "inventory.db inside zip should not be empty");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
