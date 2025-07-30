using Dimmer.DimmerLive.Models;

using ReactiveUI;

using Syncfusion.Maui.Toolkit.Carousel;

namespace Dimmer.WinUI.Views.DimmerLiveUI;

public partial class DimmerLivePage : ContentPage
{

    public DimmerLivePage(BaseViewModelWin viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = viewModel;

    }

    public BaseViewModelWin ViewModel { get; set; }
  
    private void OnSignUpClicked(object sender, EventArgs e)
    {

    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

     await   ViewModel.InitializeDimmerLiveData();
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {

    }

    private void AcceptBtn_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;

        ViewModel.DimmerLiveViewModel.AcceptFriendRequestCommand.Execute(null);
    }

    private void RejectBtn_Clicked(object sender, EventArgs e)
    {
        ViewModel.DimmerLiveViewModel.RejectFriendRequestCommand.Execute(null);

    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        ViewModel.DimmerLiveViewModel.SendFriendRequestCommand.Execute(FriendUsernameEntry.Text);
    }

    private void ConvoStart_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var param = send.BindingContext as UserModelOnline;
        ViewModel.DimmerLiveViewModel.ViewOrStartChatCommand.Execute(param!);
    }
}