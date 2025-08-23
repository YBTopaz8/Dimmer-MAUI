using Dimmer.ViewModel.DimmerLiveVM;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel;
public  partial class ChatViewModelWin : ChatViewModel
{
    public ChatViewModelWin(IChatService chatService, IFriendshipService friendshipService, IAuthenticationService auth
        ,BaseViewModel baseViewModel) : 
        base(chatService, friendshipService, auth, baseViewModel)
    {
        InitializeGeneralChat();
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
    private async Task GoToGeneralChat()
    {
         InitializeGeneralChat();
    }
}
