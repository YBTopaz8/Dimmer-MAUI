using Dimmer.DimmerLive.Models;
using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.WinUI.Views.DimmerLiveUI;

public partial class ChatView : ContentPage
{
	public ChatView( ChatViewModelWin chatViewModelWin, BaseViewModelWin baseViewModel)
	{

		InitializeComponent();
        BindingContext = chatViewModelWin;
        ChatViewModelWin=chatViewModelWin;
        BaseVM = baseViewModel;
            
    }

    public ChatViewModelWin ChatViewModelWin { get; }
    public BaseViewModelWin BaseVM{ get; set; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();


            await ChatViewModelWin.AuthenticationService.InitializeAsync();
    }

    private async void TransferSessionToDevice_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var dev = send.BindingContext as UserDeviceSession;
        if (dev is null)
            return;
        var audEngine = IPlatformApplication.Current.Services.GetService<IDimmerAudioService>();

        await ChatViewModelWin.ChatService.ShareSongAsync(audEngine.CurrentTrackMetadata,audEngine.CurrentPosition);
        //await ChatViewModelWin.SessionTransferViewModel.TransferToDevice(dev,ChatViewModelWin.MyViewModel.CurrentPlayingSongView);
    }

    private void SendMsg_Clicked(object sender, EventArgs e)
    {
        //var song = CurrentPlayingSongView;
        //await ChatViewModelWin.SendMessageCommand.ExecuteAsync(song);
    }
}