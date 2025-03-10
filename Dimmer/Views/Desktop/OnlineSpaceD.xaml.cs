using System.Diagnostics;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Views.Desktop;

public partial class OnlineSpaceD : ContentPage
{
	public OnlineSpaceD(HomePageVM homePageVM)
	{
		InitializeComponent();
        MyViewModel = homePageVM;
        MyViewModel.UserChatColView=UserChatColView;
    }
    public HomePageVM? MyViewModel { get; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        
    }
    private async void AddReaction_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var uAct = send.BindingContext as UserActivity;
        await OGSenderView.DimmInCompletely();
        OGSenderUserName.Text = uAct.Sender.Username;
        OGSenderLabel.Text = uAct.ChatMessage.Content;
        //var Msg = (UserActivity)send.BindingContext
    }

    private void EditRemoveReaction_Clicked(object sender, EventArgs e)
    {

    }

    private void MsgBorderPointerRecog_PointerEntered(object sender, PointerEventArgs e)
    {

    }

    private void MsgBorderPointerRecog_PointerExited(object sender, PointerEventArgs e)
    {

    }

    private async void SendTextMsgBtn_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(ChatMsgView.Text) || MyViewModel is null)
        {
            return;
        }
        await MyViewModel.SendMessageAsync(ChatMsgView.Text);
        ChatMsgView.Text = string.Empty;
        await OGSenderView.DimmOutCompletely();
    }

    private void SepecificUserVew_TouchDown(object sender, EventArgs e)
    {
        
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {

        await OGSenderView.DimmOutCompletely();
    }
}