using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;

namespace Dimmer.WinUI.ViewModel;
public  partial class ChatViewModelWin : ChatViewModel
{
    private readonly IChatService chatService;
    private readonly IFriendshipService friendshipService;
    private readonly IAuthenticationService auth;
    private readonly BaseViewModel baseViewModel;
    private readonly SessionManagementViewModel sessionTransferViewModel;

    public SessionManagementViewModel SessionTransferViewModel => sessionTransferViewModel;
    public BaseViewModel BaseViewModel => baseViewModel;

    public ChatViewModelWin(IChatService chatService, IFriendshipService friendshipService, LoginViewModel _loginViewModel, IAuthenticationService auth
       , BaseViewModel baseViewModel, SessionManagementViewModel sessionTransferViewModel) :
       base(chatService, friendshipService, _loginViewModel, auth, baseViewModel)
    {
        InitializeGeneralChat();
        this.chatService=chatService;
        this.friendshipService=friendshipService;
        this.auth=auth;
        this.baseViewModel=baseViewModel;
        this.sessionTransferViewModel=sessionTransferViewModel;
    }
    private async void InitializeGeneralChat()
    {
        IsBusy = true;
        if (SelectedConversation != null)
        { return; }
            var generalChat = await _chatService.GetGeneralChatAsync();
        if (generalChat != null)
        {
            // Automatically select the general chat on startup
            SelectedConversation = generalChat;
        }
        IsBusy = false;
    }

    // You can also add a command to explicitly go to the general chat
    [RelayCommand]
    private void GoToGeneralChat()
    {
         InitializeGeneralChat();
    }

    [RelayCommand]
    private async Task OpenSessionTransfer()
    {
        await sessionTransferViewModel.RegisterCurrentDeviceAsync();
    }
    [RelayCommand]
    public async Task TransferSessions(UserDeviceSession devv)
    {
        NewMessageText = $"Transfer To {devv.DeviceName}";
        await SendMessageCommand.ExecuteAsync(null);
        await sessionTransferViewModel.TransferToDevice(devv);  

        //await Shell.Current.GoToAsync("FriendsListPage");
    }
}
