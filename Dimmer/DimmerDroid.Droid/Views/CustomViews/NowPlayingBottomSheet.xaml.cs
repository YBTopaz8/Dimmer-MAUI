namespace Dimmer.Views.CustomViews;

public partial class NowPlayingBottomSheet : BottomSheet
{
	public NowPlayingBottomSheet()
	{
		InitializeComponent();
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>();
        BindingContext = MyViewModel;
	}
    BaseViewModelAnd? MyViewModel { get;}
}