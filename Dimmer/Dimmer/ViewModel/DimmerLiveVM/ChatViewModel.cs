using DynamicData;
using DynamicData.Binding;


using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

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

    [ObservableProperty]
    public partial bool IsBusy {get;set;}

    public ChatViewModel(IChatService chatService, IFriendshipService 
        
        friendshipService,

        LoginViewModel loginViewModel,
        IAuthenticationService authService
        , BaseViewModel mainViewModel)
    {
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
        _chatService.Messages.
            ObserveOn(RxSchedulers.UI)
            .Bind(out _messages)
            .Subscribe( t =>
            {
                Debug.WriteLine(t.Adds);
                Debug.WriteLine(t.Refreshes);
                Debug.WriteLine(t.Moves);
                Debug.WriteLine(t.Removes);
                Debug.WriteLine(t.Capacity);
                Debug.WriteLine(_messages.Count);
            })
            .DisposeWith(_disposables);

      
        // StartAsync chat listeners automatically
        //_chatService.StartListeners();
    }
    [RelayCommand]
    private async Task StartNoteToSelf()
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null)
        {
            await Shell.Current.DisplayAlert("Not Logged In", "Please log in to use this feature.", "OK");
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
        await Shell.Current.DisplayAlert("Song Shared", $"Shared '{currentSong.Title}' in chat.", "OK");


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
    private async Task SendMessage(SongModelView song)
    {
        //if (_authService.CurrentUserValue is null)
        //{

        //    var res = await Shell.Current.DisplayAlert("Not Logged In", "Please log in to send messages.", "Log In", "Cancel");
           

        //}
        await _chatService.SendTextMessageAsync( NewMessageText,_authService.CurrentUserValue?.ObjectId,song);
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