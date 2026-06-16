namespace FreelanceManager.App;

public static class AppPaths
{
    public static string DataDir
    {
        get
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(root, "FreelanceManager");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DatabasePath => Path.Combine(DataDir, "freelance-manager.db");
    public static string DefaultBackupDir => Path.Combine(DataDir, "backups");
}
