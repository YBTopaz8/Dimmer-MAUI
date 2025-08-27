namespace Dimmer.WinUI.Views.SettingsCenter;

public partial class SettingWin : Window
{
    public SettingWin(BaseViewModelWin vm)
    {
        InitializeComponent();
        Page = new SettingsPage(vm);
        this.Height = 800;
        this.Width = 1000;

    }
}