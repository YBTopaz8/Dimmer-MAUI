using System.Diagnostics;
using System.Linq;
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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel.CurrentPage = PageEnum.OnlineChatPage;
        if (MyViewModel.ChatMessages is not null)
        {
            UserChatColView.ScrollTo(MyViewModel.ChatMessages.Count, null, ScrollToPosition.End, true);
        }
    }
    private async void AddReaction_Clicked(object sender, EventArgs e)
    {
        ImageButton send = (ImageButton)sender;
        UserActivity? uAct = send.BindingContext as UserActivity;
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
        await MyViewModel.SendMessageAsync(ChatMsgView.Text,PlayType.ChatSent );
        ChatMsgView.Text = string.Empty;
        await OGSenderView.DimmOutCompletely();
    }

    private void SepecificUserVew_TouchDown(object sender, EventArgs e)
    {
        
    }

    private async void CloseReplyWindow_Clicked(object sender, EventArgs e)
    {

        await OGSenderView.DimmOutCompletely();
    }

    private void UserChatColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {   
        //UserChatColView.ScrollTo(UserChatColView.SelectedItem, null, ScrollToPosition.End, true);
    }

    private void UserChatColView_Loaded(object sender, EventArgs e)
    {
        MyViewModel.userChatColView = UserChatColView;

    }

    private void UserChatColView_Unloaded(object sender, EventArgs e)
    {
        MyViewModel.userChatColView= null;

    }
}