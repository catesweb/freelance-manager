using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace FreelanceManager.App.Services;

public interface IUpdateService
{
    string CurrentVersion { get; }
    Task CheckOnStartupAsync();
    Task CheckAndInstallAsync();
}

/// <summary>
/// In-app updates pulled from this repo's GitHub Releases via Velopack.
/// Only active in the installed build; a dev/unpackaged run is a no-op.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private const string RepoUrl = "https://github.com/catesweb/freelance-manager";

    private readonly INotificationService _notes;
    private readonly UpdateManager _mgr;

    public UpdateService(INotificationService notes)
    {
        _notes = notes;
        _mgr = new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
    }

    public string CurrentVersion =>
        _mgr.CurrentVersion?.ToString()
        ?? typeof(UpdateService).Assembly.GetName().Version?.ToString(3)
        ?? "dev";

    // Silent: only speaks up if an update exists. Swallows offline/no-release errors.
    public async Task CheckOnStartupAsync()
    {
        if (!_mgr.IsInstalled) return;
        try
        {
            var info = await _mgr.CheckForUpdatesAsync();
            if (info is not null)
                _notes.Show($"Update {info.TargetFullRelease.Version} available — install it from Settings.", NotificationKind.Info);
        }
        catch
        {
            // offline or no releases yet: stay quiet on startup
        }
    }

    // Manual: reports the outcome either way, then downloads + restarts into the new version.
    public async Task CheckAndInstallAsync()
    {
        if (!_mgr.IsInstalled)
        {
            _notes.Show("Updates are only available in the installed app.", NotificationKind.Info);
            return;
        }
        try
        {
            var info = await _mgr.CheckForUpdatesAsync();
            if (info is null)
            {
                _notes.Show("You're on the latest version.", NotificationKind.Success);
                return;
            }
            _notes.Show($"Downloading update {info.TargetFullRelease.Version}…", NotificationKind.Info);
            await _mgr.DownloadUpdatesAsync(info);
            _mgr.ApplyUpdatesAndRestart(info); // exits the app and relaunches the new version
        }
        catch (Exception ex)
        {
            _notes.Show($"Update failed: {ex.Message}", NotificationKind.Error);
        }
    }
}
