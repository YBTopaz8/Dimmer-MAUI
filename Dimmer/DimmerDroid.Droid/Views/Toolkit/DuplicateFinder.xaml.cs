using AndroidX.Lifecycle;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace Dimmer.Views.Toolkit;

public partial class DuplicateFinder : ContentPage
{
	public DuplicateFinder(BaseViewModelAnd vm)
	{
		InitializeComponent();
        MyViewModel = vm;
        this.BindingContext = vm;
	}

    public BaseViewModelAnd MyViewModel { get; }
    CompositeDisposable _composite;


    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);


        if (MyViewModel.DuplicateSets.Count < 1)
        {
            _ = MyViewModel.FindDuplicatesCommand.ExecuteAsync(true);
        }
        _composite = new();


        MyViewModel.WhenPropertyChange(
            nameof(MyViewModel.IsDuplicateFound),
            isBG => (MyViewModel.IsDuplicateFound))
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(
                async isDupFound =>
                {
                    if (isDupFound)
                    {
                        DuplicateScannedActionPanel.IsVisible = true ;
                    }
                    else
                    {
                        DuplicateScannedActionPanel.IsVisible = false;
                    }

                });
    }



    private void DXStackLayout_Loaded(object sender, EventArgs e)
    {
		Debugger.Break();
    }

    private void DXStackLayout_Loaded_1(object sender, EventArgs e)
    {
        Debugger.Break();
    }

    private void ActionComboBox_SelectionChanged(object sender, EventArgs e)
    {

    }
}