using FreelanceManager.Core.Services;

namespace FreelanceManager.Data;

public class BackupService : IBackupService
{
    public async Task<string> BackupAsync(string databasePath, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string name = $"freelance-manager-backup-{stamp}.db";
        string dest = Path.Combine(targetDir, name);

        using var src = File.OpenRead(databasePath);
        using var dst = File.Create(dest);
        await src.CopyToAsync(dst);
        return dest;
    }
}
