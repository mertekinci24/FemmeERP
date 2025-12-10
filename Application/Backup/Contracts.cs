namespace InventoryERP.Application.Backup;

public interface IBackupService
{
    // Creates a backup zip in the target directory; returns full path to zip
    System.Threading.Tasks.Task<string> BackupAsync(string targetDir);

    // Restores from a backup zip (full path to zip)
    System.Threading.Tasks.Task RestoreAsync(string backupZipPath);
}

public interface IBackupValidator
{
    // Validate the extracted backup directory; throw if invalid
    void Validate(string extractedDir);
}
