using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using InventoryERP.Infrastructure.Services;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using InventoryERP.Domain.Entities;

namespace Tests.Integration;

public class Restore_ReopensAndMigrates
{
    [Fact(Timeout = 60000)]
    public async Task Restore_puts_db_back_and_context_can_connect_and_tables_exist()
    {
        var root = Path.Combine(Path.GetTempPath(), "InventoryERP_Test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        try
        {
            var srcBase = Path.Combine(root, "src");
            Directory.CreateDirectory(srcBase);
            var srcDb = Path.Combine(srcBase, "inventory.db");

            // create source DB and seed
            var srcConn = $"Data Source={srcDb}";
            var srcOpt = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(srcConn)
                .Options;
            using (var ctx = new AppDbContext(srcOpt))
            {
                try { ctx.Database.EnsureDeleted(); } catch { }
                ctx.Database.EnsureCreated();
                ctx.Products.Add(new Product { Name = "RestoreProd", Sku = "RP-1", BaseUom = "pcs", VatRate = 1, Active = true });
                await ctx.SaveChangesAsync();
            }

            // ensure DB file handles are released
            GC.Collect(); GC.WaitForPendingFinalizers();
            await Task.Delay(50);

            var validator = new BackupValidator();
            var srcSvc = new BackupService(validator, srcBase);
            var outDir = Path.Combine(root, "out"); Directory.CreateDirectory(outDir);
            var zip = await srcSvc.BackupAsync(outDir);

            // restore into a separate location (no need to delete source file under test runner)
            var restoreBase = Path.Combine(root, "restore");
            Directory.CreateDirectory(restoreBase);
            var restoreSvc = new BackupService(validator, restoreBase);
            await restoreSvc.RestoreAsync(zip);

            // now open a DbContext to restored db and migrate, then check connection and tables
            var restoredDb = Path.Combine(restoreBase, "inventory.db");
            var conn = $"Data Source={restoredDb}";
            var opt = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conn)
                .Options;
            using var ctx2 = new AppDbContext(opt);
            // Ensure database is usable; restored file already contains schema
            ctx2.Database.EnsureCreated();
            Assert.True(ctx2.Database.CanConnect(), "Restored database should be connectable");
            Assert.True(await ctx2.Products.AnyAsync(), "Products table should exist and contain seeded data");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
