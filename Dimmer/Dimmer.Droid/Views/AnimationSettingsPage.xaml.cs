namespace Dimmer.Views;

public partial class AnimationSettingsPage : ContentPage
{
	public AnimationSettingsPage(AnimationSettingsViewModel animationSettingsViewModel)
	{
		InitializeComponent();
		BindingContext = animationSettingsViewModel;
        AnimationSettingsViewModel=animationSettingsViewModel;
    }

    public AnimationSettingsViewModel AnimationSettingsViewModel { get; }
}