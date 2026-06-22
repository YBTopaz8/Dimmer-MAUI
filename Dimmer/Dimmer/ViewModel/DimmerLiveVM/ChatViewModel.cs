namespace Dimmer.ViewModel.DimmerLiveVM;
public partial class ChatViewModel : ObservableObject, IDisposable
{
    public IChatService ChatService => _chatService;

    public IAuthenticationService AuthenticationService => _authService;
    public readonly IChatService _chatService;
    private readonly IFriendshipService _friendshipService;
    private readonly LoginViewModel loginViewModel;
    private readonly IAuthenticationService _authService;
    private readonly BaseViewModel _mainViewModel;
    private readonly CompositeDisposable _disposables = new();

    // --- UI-Bound Collections ---
    private readonly ReadOnlyObservableCollection<ChatConversation> _conversations;
    public ReadOnlyObservableCollection<ChatConversation> Conversations => _conversations;

    private ReadOnlyObservableCollection<ChatMessage> _messages;
    public ReadOnlyObservableCollection<ChatMessage> Messages => _messages;
   

    [ObservableProperty]
    public partial ObservableCollection<UserModelOnline> UserSearchResults { get; set; }

    // --- UI State ---
    [ObservableProperty]
    public partial ChatConversation SelectedConversation{get;set;}
    

    [ObservableProperty]
    public partial string NewMessageText{get;set;}



    [ObservableProperty]
    public partial string UserSearchTerm{get;set;}
    private IDisposable? _currentMessagesSubscription;
    [ObservableProperty]
    public partial bool IsBusy {get;set; }
    ILiveSessionManagerService _sessionManager;
    private readonly ReadOnlyObservableCollection<UserDeviceSession> _connectedDevices;
    public ReadOnlyObservableCollection<UserDeviceSession> ConnectedDevices => _connectedDevices;

    [ObservableProperty]
    public partial UserDeviceSession SelectedDevice { get; set; }
    public ChatViewModel(IChatService chatService, IFriendshipService 
        
        friendshipService,
        ILiveSessionManagerService sessionMgr,

        LoginViewModel loginViewModel,
        IAuthenticationService authService
        , BaseViewModel mainViewModel)
    {
        _sessionManager = sessionMgr;
        _chatService = chatService;
        _friendshipService = friendshipService;
        this.loginViewModel=loginViewModel;
        this._authService=authService;
        this._mainViewModel=mainViewModel;

        // Bind the service's conversations to our UI collection
        _chatService.Conversations
            .Sort(SortExpressionComparer<ChatConversation>.Descending(c => c.LastMessageTimestamp))
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _conversations)
            .Subscribe()
            .DisposeWith(_disposables);


        _sessionManager.OtherAvailableDevices
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _connectedDevices)
            .Subscribe()
            .DisposeWith(_disposables);

        // StartAsync chat listeners automatically
        //_chatService.StartListeners();
        LoadGeneralChatCommand.ExecuteAsync(null);
    }
    [RelayCommand]
    private async Task LoadGeneralChat()
    {
        IsBusy = true;
        var generalChat = await _chatService.GetGeneralChatAsync();
        if (generalChat != null)
        {
            SelectedConversation = generalChat; // This triggers OnSelectedConversationChanged
        }
        IsBusy = false;
    }
    partial void OnSelectedConversationChanged(ChatConversation value)
    {
        if (value == null) return;

        // Dispose previous message listener so UI doesn't show old messages
        _currentMessagesSubscription?.Dispose();

        // Bind the newly selected conversation's messages to the UI collection
        _currentMessagesSubscription = _chatService.GetMessagesForConversation(value)
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _messages) // Updates your ReadOnlyObservableCollection
            .Subscribe(v=>
            {
                Debug.WriteLine($"Messages COunt {v.Count}");
        })
            .DisposeWith(_disposables);

        RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Messages)));
    }
    partial void OnSelectedDeviceChanged(UserDeviceSession value)
    {
        if (value == null) return;

        // Generate a localized or Cloud-Code backed Conversation for this device pair
        // E.g., GetOrCreateDeviceConversationAsync(value.DeviceId)
        // For now, let's pretend we have it, which switches the Chat View to the Device Terminal!
       _= CreateAndSwitchToDeviceTerminalAsync(value);
    }
    private async Task CreateAndSwitchToDeviceTerminalAsync(UserDeviceSession device)
    {
        // Call your chat service to get a special 1-on-1 chat for "My Phone <-> My PC"
        // This keeps message history between devices!
        var terminalConvo = await _chatService.GetOrCreateConversationWithUserAsync(_authService.CurrentUserValue); // Simplified
        if (terminalConvo is not null)
        {
            terminalConvo.Name = $"Terminal: {device.DeviceName}";

            SelectedConversation = terminalConvo;
        }
    }
    [RelayCommand]
    private async Task StartNoteToSelf()
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null)
        {
            await Shell.Current.DisplayAlertAsync("Not Logged In", "Please log in to use this feature.", "OK");
            // Optionally, show a message telling the user to log in
            return;
        }

        // We call the existing command, passing the current user's own object.
        await FindAndStartChatCommand.ExecuteAsync(currentUser);
    }
    [RelayCommand]
    private async Task ShareCurrentSong()
    {
        var currentSong = _mainViewModel.CurrentPlayingSongView;
        if (SelectedConversation == null || currentSong == null || currentSong.Id == ObjectId.Empty)
        {
            // Optionally, show a "nothing is playing" message
            return;
        }

        // The service handles all the backend logic
        await _chatService.ShareSongAsync( currentSong, _mainViewModel.CurrentTrackPositionSeconds);
        // Optionally, show a "Song shared!" notification
        await Shell.Current.DisplayAlertAsync("Song Shared", $"Shared '{currentSong.Title}' in chat.", "OK");


    }
    [RelayCommand]
    private async Task FindAndStartChat(UserModelOnline user)
    {
        if (user == null)
            return;

        IsBusy = true;
        // The service handles all the logic of finding or creating the conversation
        var conversation = await _chatService.GetOrCreateConversationWithUserAsync(user);
        if (conversation != null)
        {
            SelectedConversation = conversation;
        }
        UserSearchTerm = string.Empty; // Clear search after starting a chat
        UserSearchResults?.Clear();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        //if (_authService.CurrentUserValue is null)
        //{

        //    var res = await Shell.Current.DisplayAlertAsync("Not Logged In", "Please log in to send messages.", "Log In", "Cancel");


        //}
        await _chatService.SendTextMessageAsync(SelectedConversation,NewMessageText, _authService.CurrentUserValue?.ObjectId);
        NewMessageText = string.Empty; // Clear the input box
    }

    // This method is called by the UI as the user types in the search box
    partial void OnUserSearchTermChanged(string value)
    {
        // Debounce to avoid excessive searching while typing
        Observable.Return(value)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .SelectMany(term => _friendshipService.FindUsersAsync(term))
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(results =>
            {
                UserSearchResults = new ObservableCollection<UserModelOnline>(results);
            })
            .DisposeWith(_disposables); // Auto-dispose previous search subscription
    }

    public void Dispose()
    {
        _chatService.StopListeners();
        _disposables.Dispose();
    }

    public void RaisePropertyChanging(System.ComponentModel.PropertyChangingEventArgs args)
    {

        OnPropertyChanging(args);
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        OnPropertyChanged(args);
    }
}