using DevExpress.Maui.Controls;

namespace Dimmer.Views.CustomViewsParts;

public partial class QuickPanelBtmSheet : BottomSheet
{
	public QuickPanelBtmSheet()
	{
		InitializeComponent();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()??throw new NullReferenceException("BaseViewModelAnd is not registered in the service collection.");
        this.BindingContext =vm;

        this.MyViewModel =vm;
    }
    public BaseViewModelAnd MyViewModel { get; set; }
}