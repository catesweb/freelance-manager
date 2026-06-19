namespace FreelanceManager.App.Services;

public interface IAppStateService
{
    bool OnboardingDismissed { get; }
    void DismissOnboarding();
}
