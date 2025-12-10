using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using InventoryERP.Application.Backup;

namespace InventoryERP.Infrastructure.Services
{
    public class BackupService : IBackupService
    {
        private readonly IBackupValidator _validator;
        private readonly string _basePath;

        public BackupService(IBackupValidator validator, string? basePath = null)
        {
            _validator = validator;
            _basePath = basePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InventoryERP");
        }

        private string DbPath => Path.Combine(_basePath, "inventory.db");

        public Task<string> BackupAsync(string targetDir)
        {
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var name = $"inventory_{timestamp}.zip";
            var dest = Path.Combine(targetDir, name);

            // if DB uses WAL, ensure we checkpoint so main DB file contains latest changes
            if (File.Exists(DbPath))
            {
                try
                {
                    // lightweight checkpoint to flush WAL into the main DB file
                    var cs = $"Data Source={DbPath}";
                    using var conn = new Microsoft.Data.Sqlite.SqliteConnection(cs);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    // best-effort; if checkpoint fails, continue and attempt to read the file
                }
            }

            using (var zip = ZipFile.Open(dest, ZipArchiveMode.Create))
            {
                if (File.Exists(DbPath))
                {
                    // open DB for read with shared access to avoid file-in-use errors
                    var entry = zip.CreateEntry("inventory.db", CompressionLevel.Optimal);
                    using var src = File.Open(DbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var dst = entry.Open();
                    src.CopyTo(dst);
                }

                var cfg = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (File.Exists(cfg))
                {
                    var entry2 = zip.CreateEntry("appsettings.json", CompressionLevel.Optimal);
                    using var src2 = File.Open(cfg, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var dst2 = entry2.Open();
                    src2.CopyTo(dst2);
                }
            }

            // quick sanity
            var fi = new FileInfo(dest);
            if (!fi.Exists || fi.Length == 0) throw new InvalidOperationException("Backup failed: archive empty");
            return Task.FromResult(dest);
        }

        public Task RestoreAsync(string backupZipPath)
        {
            if (!File.Exists(backupZipPath)) throw new FileNotFoundException("Backup not found", backupZipPath);
            var tmp = Path.Combine(Path.GetTempPath(), "InventoryERP_Restore", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tmp);
            ZipFile.ExtractToDirectory(backupZipPath, tmp);

            // validate
            _validator.Validate(tmp);

            var extractedDb = Path.Combine(tmp, "inventory.db");
            if (!File.Exists(extractedDb)) throw new InvalidOperationException("Backup archive does not contain inventory.db");

            // ensure base dir
            Directory.CreateDirectory(_basePath);

            var dest = DbPath;
            if (File.Exists(dest))
            {
                var bak = dest + ".bak." + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                File.Move(dest, bak);
            }

            File.Move(extractedDb, dest);

            // attempt migrate if possible: caller may run migrations; we don't have direct DbContext here to avoid heavy deps
            // leave migration to host/startup
            return Task.CompletedTask;
        }
    }
}
