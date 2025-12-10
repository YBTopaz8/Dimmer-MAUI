using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.WinUI.Views.WinuiPages.DimmerLive;

namespace Dimmer.WinUI.ViewModel.DimmerLiveWin;

public partial class LoginViewModelWin : LoginViewModel
{
    public LoginViewModelWin(IAuthenticationService authService, IFilePicker _filePicker, IRealmFactory realmFactory,BaseViewModelWin baseViewModel) : base(authService, _filePicker, realmFactory)
    {
        BaseViewModel = baseViewModel;
    }

    public BaseViewModelWin BaseViewModel { get; }

    internal void NavigateToProfilePage()
    {
        BaseViewModel.NavigateToAnyPageOfGivenType(typeof(LoginPage));
    }
}
