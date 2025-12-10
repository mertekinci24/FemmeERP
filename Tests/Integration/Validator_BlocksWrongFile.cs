using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Xunit;
using InventoryERP.Infrastructure.Services;

namespace Tests.Integration;

public class Validator_BlocksWrongFile
{
    [Fact(Timeout = 30000)]
    public async Task Restore_throws_on_zip_without_inventory_db()
    {
        var root = Path.Combine(Path.GetTempPath(), "InventoryERP_Test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        try
        {
            var zip = Path.Combine(root, "wrong.zip");
            using (var za = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("readme.txt");
                using var s = e.Open();
                using var sw = new StreamWriter(s);
                sw.Write("no db here");
            }

            var validator = new BackupValidator();
            var svc = new BackupService(validator, Path.Combine(root, "appdata"));

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.RestoreAsync(zip));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
