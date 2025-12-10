using System;
using System.IO;
using InventoryERP.Application.Backup;

namespace InventoryERP.Infrastructure.Services
{
    public class BackupValidator : IBackupValidator
    {
        public void Validate(string extractedDir)
        {
            // Basic checks: inventory.db exists and is non-empty
            var db = Path.Combine(extractedDir, "inventory.db");
            if (!File.Exists(db)) throw new InvalidOperationException("Extracted backup does not contain inventory.db");
            var fi = new FileInfo(db);
            if (fi.Length == 0) throw new InvalidOperationException("inventory.db in backup is empty");
            // Additional validations (PRAGMA schema_version etc.) could be added here
        }
    }
}
