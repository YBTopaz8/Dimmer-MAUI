using Dimmer.DimmerLive.Models;
using Dimmer.DimmerLive.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;
public partial class DimmerLiveViewModel : ObservableObject
{

    private readonly IMapper _mapper;
    [ObservableProperty]
    public partial bool IsConnected { get; set; } = false;
    
    [ObservableProperty]
    public partial bool IsOnline { get; set; } = false;
    [ObservableProperty]
    public partial ParseUser? UserOnline { get; set; }
    [ObservableProperty]
    public partial UserModelView? UserLocal { get; set; }


    BaseViewModel baseViewModel;

    public DimmerLiveViewModel(IMapper mapper, BaseViewModel vm)
    {

        baseViewModel = vm;
        InitializeClass();
    }

    private void InitializeClass()
    {
        
        if (ParseSetup.InitializeParseClient())
        {
            ParseClient.Instance.RegisterSubclass(typeof(UserDeviceSession));
            ParseClient.Instance.RegisterSubclass(typeof(ChatConversation));
            ParseClient.Instance.RegisterSubclass(typeof(ParseSong));
            ParseClient.Instance.RegisterSubclass(typeof(UserModelOnline));
        }
        UserLocal = _mapper.Map<UserModelView>(BaseAppFlow.CurrentUser);

    }

    public void SaveUserEdit()
    {
        
    }
}
