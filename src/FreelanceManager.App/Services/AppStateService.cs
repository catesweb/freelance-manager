using System.IO;
using System.Text.Json;

namespace FreelanceManager.App.Services;

public class AppStateService : IAppStateService
{
    private record State(bool OnboardingDismissed);
    private readonly string _path;
    private State _state;

    public AppStateService(string path)
    {
        _path = path;
        _state = File.Exists(_path)
            ? JsonSerializer.Deserialize<State>(File.ReadAllText(_path)) ?? new State(false)
            : new State(false);
    }

    public bool OnboardingDismissed => _state.OnboardingDismissed;

    public void DismissOnboarding()
    {
        _state = _state with { OnboardingDismissed = true };
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(_state));
    }
}
