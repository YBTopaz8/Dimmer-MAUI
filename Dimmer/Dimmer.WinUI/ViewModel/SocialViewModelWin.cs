namespace Dimmer.WinUI.ViewModel;
public class SocialViewModelWin : SocialViewModel
{
    public SocialViewModelWin(IFriendshipService friendshipService, IAuthenticationService authService) : base(friendshipService, authService)
    {
    }
}
