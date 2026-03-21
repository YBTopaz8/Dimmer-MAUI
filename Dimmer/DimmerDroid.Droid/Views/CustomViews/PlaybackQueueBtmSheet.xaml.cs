global using DevExpress.Maui.Controls;

namespace Dimmer.Views.CustomViews;

public partial class PlaybackQueueBtmSheet : BottomSheet
{
	public PlaybackQueueBtmSheet()
	{
		InitializeComponent();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>();
	BindingContext = vm;

        MyViewModel = vm;
	}
    BaseViewModelAnd? MyViewModel { get;}
}