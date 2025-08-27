using Dimmer.DimmerSearch.Interfaces;
using Dimmer.ViewModel;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel;
public partial class SessionTransferVMWin : SessionTransferViewModel
{
    public LoginViewModel LoginViewModel;
    public SessionTransferVMWin(LoginViewModel loginViewModel, ILiveSessionManagerService sessionManager, ILogger<SessionTransferViewModel> logger, BaseViewModel mainViewModel) : base(sessionManager, logger, mainViewModel)
    {
        LoginViewModel = loginViewModel?? throw new ArgumentNullException(nameof(loginViewModel));
        // Initialize any additional properties or commands specific to the WinUI implementation here
    }

    public bool IsAuthenticated ()
    {

        var isAuth = LoginViewModel.CurrentUser != null && !string.IsNullOrEmpty(LoginViewModel.CurrentUser.SessionToken);
        if (!isAuth)
        {
            LoginViewModel.ErrorMessage = "You must be logged in to transfer sessions.";
        }
        return isAuth;
    }
}
