
namespace Dimmer.WinUI.Utils.WinMgt;

public interface ISettingsWindowManager
{
    void ShowSettingsWindow(BaseViewModelWin viewModel);
    void BringSettingsWindowToFront();
    void CloseSettingsWindow();
    bool IsSettingsWindowOpen { get; }
    SettingsWindow InstanceWindow { get; }
}