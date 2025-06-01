﻿namespace Dimmer.Interfaces.Services.Interfaces;

public interface IAppInitializerService
{
    Task InitializeApplicationAsync();
    Task LoadApplicationStateAsync(); // Loads from repo, updates _state.SetApplicationSettingsState
    Task SaveApplicationStateAsync(AppStateModelView appStateView);
    // IObservable<AppStateModelView?> CurrentAppState { get; } // Could be here or just via IDimmerStateService
}