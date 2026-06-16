using System;
using System.IO;
using System.Threading.Tasks;
using FreelanceManager.Core.Services;
using FreelanceManager.Data;
using Xunit;

namespace FreelanceManager.Tests;

public class BackupServiceTests
{
    [Fact]
    public async Task Backup_copies_db_file_into_target_folder_with_timestamp()
    {
        string temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(temp);
        string dbPath = Path.Combine(temp, "app.db");
        await File.WriteAllTextAsync(dbPath, "SQLITE");
        string targetDir = Path.Combine(temp, "backups");

        IBackupService svc = new BackupService();
        string backupPath = await svc.BackupAsync(dbPath, targetDir);

        Assert.True(File.Exists(backupPath));
        Assert.StartsWith(targetDir, backupPath);
        Assert.Equal("SQLITE", await File.ReadAllTextAsync(backupPath));

        Directory.Delete(temp, recursive: true);
    }
}
