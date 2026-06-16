namespace FreelanceManager.Core.Services;

public interface IBackupService
{
    /// <summary>Copies the database file into targetDir, returns the new file path.</summary>
    Task<string> BackupAsync(string databasePath, string targetDir);
}
