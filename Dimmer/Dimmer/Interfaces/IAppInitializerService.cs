namespace Dimmer.Interfaces;

public interface IAppInitializerService
{
    void InitializeApplication();
    Task LoadApplicationStateAsync(); // Loads from repo, updates _stateService.SetApplicationSettingsState
    Task SaveApplicationStateAsync(AppStateModelView appStateView);
    // IObservable<AppStateModelView?> CurrentAppState { get; } // Could be here or just via IDimmerStateService
}