using Dimmer.DimmerSearch.Interfaces;

using Microsoft.Extensions.Logging;

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
