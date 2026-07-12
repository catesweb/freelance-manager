namespace FreelanceManager.App;

public static class AppPaths
{
    public static string DataDir
    {
        get
        {
            // Override for tests/portable use; normal installs use %AppData%.
            string dir = Environment.GetEnvironmentVariable("FREELANCEMANAGER_DATA_DIR")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "FreelanceManager");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DatabasePath => Path.Combine(DataDir, "freelance-manager.db");
    public static string DefaultBackupDir => Path.Combine(DataDir, "backups");
}
