
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel;
public class SocialViewModelWin : SocialViewModel
{
    public SocialViewModelWin(IFriendshipService friendshipService, IAuthenticationService authService) : base(friendshipService, authService)
    {
    }
}
