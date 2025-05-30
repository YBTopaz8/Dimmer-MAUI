using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.IServices;

public interface IAppInitializerService
{
    Task InitializeApplicationAsync();
    Task LoadApplicationStateAsync(); // Loads from repo, updates _state.SetApplicationSettingsState
    Task SaveApplicationStateAsync(AppStateModelView appStateView);
    // IObservable<AppStateModelView?> CurrentAppState { get; } // Could be here or just via IDimmerStateService
}