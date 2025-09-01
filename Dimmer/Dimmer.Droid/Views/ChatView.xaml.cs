namespace Dimmer.Views;

public partial class ChatView : ContentPage
{
	public ChatView(ChatViewModelAnd chatViewModelAnd, BaseViewModelAnd baseViewModel)
    {

        InitializeComponent();
        BindingContext = chatViewModelAnd;
        ChatViewModelAnd=chatViewModelAnd;
        BaseVM = baseViewModel;

    }

    public ChatViewModelAnd ChatViewModelAnd { get; }
    public BaseViewModelAnd BaseVM { get; set; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        ChatViewModelAnd.ChatService.StartListeners();

        //await ChatViewModelAnd.AuthenticationService.InitializeAsync();
    }

    private void TransferSessionToDevice_Clicked(object sender, EventArgs e)
    {

    }

    private async void SendMsg_Clicked(object sender, EventArgs e)
    {
        //var song = m CurrentPlayingSongView;
        //await ChatViewModelAnd.SendMessageCommand.ExecuteAsync(song);
    }

    private void BtmBar_RequestFocusNowPlayingUI(object sender, EventArgs e)
    {

    }

    private void BtmBar_RequestFocusOnMainView(object sender, EventArgs e)
    {

    }

    private void BtmBar_ScrollToStart(object sender, EventArgs e)
    {

    }

    private void BtmBar_ToggleAdvanceFilters(object sender, EventArgs e)
    {

    }

    private void DXCollectionView_SizeChanged(object sender, EventArgs e)
    {

    }
}