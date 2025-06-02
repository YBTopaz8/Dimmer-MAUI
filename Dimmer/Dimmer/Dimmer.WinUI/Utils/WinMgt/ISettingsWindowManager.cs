
namespace Dimmer.WinUI.Utils.WinMgt;

public interface ISettingsWindowManager
{
    void ShowSettingsWindow(BaseViewModel viewModel);
    void BringSettingsWindowToFront();
    void CloseSettingsWindow();
    bool IsSettingsWindowOpen { get; }
    SettingWindow InstanceWindow { get; }
}