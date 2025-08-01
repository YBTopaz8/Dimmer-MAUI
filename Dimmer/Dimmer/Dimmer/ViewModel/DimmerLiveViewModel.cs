using CommunityToolkit.Mvvm.Input;

using Dimmer.Charts;
using Dimmer.DimmerLive.Interfaces.Services;

using DynamicData;
using DynamicData.Binding;

using Parse.LiveQuery;

using ReactiveUI;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;
// Inherits from ReactiveObject for FRP capabilities.
public partial class DimmerLiveViewModel :ObservableObject, IReactiveObject, IActivatableViewModel
{
    private readonly IDeviceConnectivityService deviceConnectivityService;
    private readonly BaseViewModel viewModel;
    private readonly IAuthenticationService _authService;
    private readonly ReadOnlyObservableCollection<DeviceState> _availablePlayers;
    private readonly CompositeDisposable _disposables = new();
    // --- PUBLIC PROPERTIES FOR UI BINDING ---

    [ObservableProperty]
    public partial UserModelOnline? CurrentUser { get; set; }
    public ReadOnlyObservableCollection<DeviceState> AvailablePlayers => _availablePlayers;

    // A ReactiveUI style read/write property.
    private DeviceState? _controlledDeviceState;
    public DeviceState? ControlledDeviceState
    {
        get => _controlledDeviceState;
        set => this.RaiseAndSetIfChanged(ref _controlledDeviceState, value);
    }

    // --- OUTPUT PROPERTIES (The heart of a Reactive ViewModel) ---

    // An "Output Property" that is the result of an observable stream.
    // It will automatically update whenever the stream produces a new value.
    private readonly ObservableAsPropertyHelper<string> _mirroredPlaybackState;
    public string MirroredPlaybackState => _mirroredPlaybackState.Value;

    private readonly ObservableAsPropertyHelper<string> _mirroredCurrentSongId;
    public string MirroredCurrentSongId => _mirroredCurrentSongId.Value;

    // --- REACTIVE COMMANDS ---

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> PlayPauseCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NextTrackCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> PreviousTrackCommand { get; }

    // This is for IActivatableViewModel. It's a container for all subscriptions
    // that are active only when the ViewModel is "activated" (e.g., when the page appears).
    public ViewModelActivator Activator { get; } = new();

    // --- CONSTRUCTOR ---

    public DimmerLiveViewModel(IDeviceConnectivityService deviceConnectivityService,
        BaseViewModel viewModel,

    IAuthenticationService authService)
    {
        this.deviceConnectivityService=deviceConnectivityService;
        this.viewModel=viewModel;
        _authService = authService;

        _messagesCache.Connect()
            
      .ObserveOn(RxApp.MainThreadScheduler)
      .Bind(out _messagesInGroupConversation) // Ensure this variable name matches your class field
      .Subscribe()
      .DisposeWith(_disposables);








        //_deviceConnectivityService = deviceConnectivityService;

        //// --- DECLARATIVE SETUP IN THE CONSTRUCTOR ---

        //// 1. Bind the list of available players from the service to our public property.
        //_deviceConnectivityService.AvailablePlayers
        //    .ObserveOn(RxApp.MainThreadScheduler)
        //    .Bind(out _availablePlayers)
        //    .Subscribe(); // This subscription is permanent for the life of the ViewModel.

        //// 2. Define the conditions under which the remote control commands can be executed.
        ////    The command can only execute when `ControlledDeviceState` is not null.
        //var canExecuteRemoteCommand = this.WhenAnyValue(vm => vm.ControlledDeviceState)
        //                                  .Select(state => state != null);

        //// 3. Create the ReactiveCommands.
        //NextTrackCommand = ReactiveCommand.CreateFromTask(
        //    () => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "NEXT"),
        //    canExecuteRemoteCommand
        //);
        //PreviousTrackCommand = ReactiveCommand.CreateFromTask(
        //    () => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "PREVIOUS"),
        //    canExecuteRemoteCommand
        //);
        //PlayPauseCommand = ReactiveCommand.CreateFromTask(
        //    () => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "PLAY_PAUSE"),
        //    canExecuteRemoteCommand
        //);

        // 4. This is the core logic for "State Mirroring".
        //    We create an observable stream that reacts whenever the user selects a different device.
        var controlledDeviceStream = this.WhenAnyValue(vm => vm.ControlledDeviceState);

        // 5. We use the `Switch` operator to manage the Live Query subscription.
        //    `Switch` is incredibly powerful: it automatically unsubscribes from the old device's
        //    update stream and subscribes to the new one whenever `controlledDeviceStream` changes.
        //var stateUpdateStream = controlledDeviceStream
        //    .Select(device =>
        //    {
        //        if (device == null)
        //        {
        //            // If no device is selected, return an empty stream.
        //            return Observable.Empty<DeviceState>();
        //        }
        //        // For the selected device, create a new observable stream from its Live Query updates.
        //        return Observable.Create<DeviceState>(async observer =>
        //        {
        //            var query = new ParseQuery<DeviceState>(ParseClient.Instance).WhereEqualTo("objectId", device.ObjectId);
        //            var sub = await _deviceConnectivityService.LiveQueryClient.SubscribeAsync(query);

        //            var updateDisposable = sub.On(Subscription.Event.Update, (updatedState, original) =>
        //            {
        //                observer.OnNext(updatedState);
        //            });
        //            // Return a disposable that will automatically unsubscribe when Switch moves to the next device.
        //            return new CompositeDisposable(updateDisposable, Disposable.Create(() => sub.UnsubscribeNow()));
        //        });
        //    })
        //    .Switch(); // This is the magic that manages the subscriptions automatically.

        //// 6. Bind the output of our state update stream to our public properties.
        //_mirroredPlaybackState = stateUpdateStream
        //    .Select(state => state.PlaybackState)
        //    .StartWith("Not Connected") // Provide an initial value
        //    .ToProperty(this, vm => vm.MirroredPlaybackState);

        //_mirroredCurrentSongId = stateUpdateStream
        //    .Select(state => state.CurrentSongId)
        //    .StartWith(string.Empty)
        //    .ToProperty(this, vm => vm.MirroredCurrentSongId);

        // --- LIFECYCLE MANAGEMENT using IActivatableViewModel ---

        //this.WhenActivated(async disposables =>
        //{
        //    // This code runs when the View is shown.
        //    await _deviceConnectivityService.InitializeAsync(); 
        //    _deviceConnectivityService.StartListeners();

        //    // This code runs when the View is hidden.
        //    Disposable.Create(() => _deviceConnectivityService.StopListeners()).DisposeWith(disposables);
        //});

        LoadAllUsersCommand = ReactiveCommand.CreateFromTask(LoadAllUsersInternalAsync);
        ViewOrStartChatCommand = ReactiveCommand.CreateFromTask<UserModelOnline, ChatConversation>(
             user => GetOrCreateConversationWithUserInternalAsync(user)
         );

        ViewOrStartChatCommand.Subscribe(conversation =>
        {
            if (conversation != null)
            {
                SelectedConversation = conversation;
                //var ee = SelectedConversation.
                // You would also trigger navigation to the chat tab here.
                 SelectedIndex= 2; 
            }
        });

        var friendsSort = SortExpressionComparer<UserModelOnline>.Ascending(u => u.Username);
        _friendsCache.Connect().Sort(friendsSort).ObserveOn(RxApp.MainThreadScheduler).Bind(out _friends).Subscribe().DisposeWith(_disposables);

        var requestSort = SortExpressionComparer<FriendRequest>.Descending(r => r.CreatedAt);
        _friendRequestsCache.Connect().Sort(requestSort).ObserveOn(RxApp.MainThreadScheduler).Bind(out _friendRequests).Subscribe().DisposeWith(_disposables);

        var convoSort = SortExpressionComparer<ChatConversation>.Descending(c => c.LastMessageTimestamp);
        _conversationsCache.Connect().Sort(convoSort).ObserveOn(RxApp.MainThreadScheduler).Bind(out _conversations).Subscribe().DisposeWith(_disposables);


        #region --- Command Implementation ---


        #region --- Command Implementation ---

        //NextTrackCommand = ReactiveCommand.CreateFromTask(() => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "NEXT"), canExecuteRemoteCommand);
        //PreviousTrackCommand = ReactiveCommand.CreateFromTask(() => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "PREVIOUS"), canExecuteRemoteCommand);
        //PlayPauseCommand = ReactiveCommand.CreateFromTask(() => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "PLAY_PAUSE"), canExecuteRemoteCommand);

        SendFriendRequestCommand = ReactiveCommand.CreateFromTask<string, AuthResult>(SendFriendRequestInternalAsync);
        AcceptFriendRequestCommand = ReactiveCommand.CreateFromTask<FriendRequest>(AcceptFriendRequestInternalAsync);
        RejectFriendRequestCommand = ReactiveCommand.CreateFromTask<FriendRequest>(RejectFriendRequestInternalAsync);

        SendMessageCommand = ReactiveCommand.CreateFromTask(()=>SendMessageInternalAsync());

        EditMessageCommand = ReactiveCommand.CreateFromTask<ChatMessage>(EditMessageInternalAsync);
        DeleteMessageCommand = ReactiveCommand.CreateFromTask<ChatMessage>(DeleteMessageInternalAsync);
        ReactToMessageCommand = ReactiveCommand.CreateFromTask<ChatMessage>(message => ReactToMessageInternalAsync(message, "❤️"));

     
        #endregion

        #region --- Activation Logic ---



        //Task.Run(SetupLiveQueryListeners);
        #endregion
    }
    [RelayCommand]
    public async Task LoadUsers()

    {
        var query = new ParseQuery<UserModelOnline>(ParseClient.Instance);
        var res = await query.FindAsync();
        var usrs = res.ToObservableCollection();
        OnlineUsers ??=new();
        OnlineUsers?.Clear();
        OnlineUsers?.AddRange(usrs);    
    }
    [ObservableProperty]
    public partial ObservableCollection< UserModelOnline> OnlineUsers { get; set; }
    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    async partial void OnSelectedIndexChanged(int value)
    {
        if (value == 2)
        {

            CurrentUser = _authService.CurrentUser;
            await SetupLiveQueries();
        }
        if (value == 3)
        {
            await LoadUsers();
        }
    }
  


    private readonly SourceCache<UserModelOnline, string> _friendsCache = new(user => user.ObjectId);
    private readonly SourceCache<FriendRequest, string> _friendRequestsCache = new(req => req.ObjectId);
    private readonly SourceCache<ChatConversation, string> _conversationsCache = new(chat => chat.ObjectId);
    private readonly SourceCache<ChatMessage, string> _messagesCache = new(msg => msg.ObjectId);

    private readonly ReadOnlyObservableCollection<UserModelOnline> _friends;
    private readonly ReadOnlyObservableCollection<UserModelOnline> _userss;
    private readonly ReadOnlyObservableCollection<FriendRequest> _friendRequests;
    private readonly ReadOnlyObservableCollection<ChatConversation> _conversations;
    private readonly ReadOnlyObservableCollection<ChatMessage> _messagesInGroupConversation;
    public ReadOnlyObservableCollection<ChatMessage> MessagesInGroupConversation => _messagesInGroupConversation;

    public ReadOnlyObservableCollection<UserModelOnline> Friends => _friends;
    public ReadOnlyObservableCollection<UserModelOnline> Users => _userss;
    public ReadOnlyObservableCollection<FriendRequest> FriendRequests => _friendRequests;
    public ReadOnlyObservableCollection<ChatConversation> Conversations => _conversations;

    [ObservableProperty] public partial ChatConversation? SelectedConversation { get; set; }
    [ObservableProperty] public partial string NewMessageText {get;set;}
    [ObservableProperty] public partial string LatestMessageType {get;set;}
    [ObservableProperty] public partial bool IsSocialHubActive {get;set;}


    public ReactiveCommand<string, AuthResult> SendFriendRequestCommand { get; }
    public ReactiveCommand<FriendRequest, Unit> AcceptFriendRequestCommand { get; }
    public ReactiveCommand<FriendRequest, Unit> RejectFriendRequestCommand { get; }
    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }
    public ReactiveCommand<ChatMessage, Unit> EditMessageCommand { get; }
    public ReactiveCommand<ChatMessage, Unit> DeleteMessageCommand { get; }
    public ReactiveCommand<ChatMessage, Unit> ReactToMessageCommand { get; } // Assumes a payload from CommandParameter
                                                                             // This property will hold the user that the app user clicks on in the list.
    [ObservableProperty]
    public partial UserModelOnline? SelectedUserFromList { get; set; }

    // This command will be triggered to load or refresh the list of all users.
    public ReactiveCommand<Unit, Unit> LoadAllUsersCommand { get; }

    // This command is triggered when a user is selected from the list.
    public ReactiveCommand<UserModelOnline, ChatConversation> ViewOrStartChatCommand { get; }
    [ObservableProperty] public partial ObservableCollection<UserModelOnline>? AllUsers { get; set; }
    [ObservableProperty] public partial ObservableCollection<ChatMessage>? AllMessages { get; set; }
    [ObservableProperty] public partial bool IsConnected { get; set; }





    [RelayCommand]
    public async Task FindAllMessagesInconvo(ChatConversation conversation)
    {
        //    var query = new ParseQuery<ChatMessage>(ParseClient.Instance)
        //        .WhereRelatedTo(conversation, CurrentUser.ObjectId);
        ////.WhereEqualTo("conversation", selectedConversation);

        //    var messages = await query.FindAsync();
        var messages = await new ParseQuery<ChatMessage>(ParseClient.Instance)
            .Include(nameof(ChatMessage.Conversation)).FindAsync();

        var rr = messages.ToList();
    //.WhereEqualTo("conversation", conversation)
    //.OrderBy("createdAt")
    //.FindAsync();

        AllMessages ??= new ObservableCollection<ChatMessage>();
        AllMessages.Clear();
        foreach (var message in messages)
        {
            AllMessages.Add(message);
        }


    }

    #endregion
    #region --- Internal Business Logic ---


    /// <summary>
    /// Gets a stable, unique ID for the sender.
    /// - For logged-in users, this is their permanent ObjectId.
    /// - For anonymous users, this is the ObjectId of their app's installation.
    /// </summary>
    /// <returns>A unique sender ID string.</returns>
   
    private async Task LoadAllUsersInternalAsync()
    {
        CurrentUser= _authService.CurrentUser;

        // Query for all users *except* the current user.
        var query = new ParseQuery<UserModelOnline>(ParseClient.Instance)
            .WhereNotEqualTo("objectId", CurrentUser.ObjectId);

        var users = await query.FindAsync();

        // Update the UI collection on the main thread.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AllUsers ??=new ObservableCollection<UserModelOnline>();
            AllUsers.Clear();
            foreach (var user in users)
            {
                AllUsers.Add(user);
            }
        });
        SelectedIndex= 2;

    }

    // In your DimmerLiveViewModel.cs

    private async Task<ChatConversation> GetOrCreateConversationWithUserInternalAsync(UserModelOnline otherUser)
    {
        var currentUser = (UserModelOnline)ParseUser.CurrentUser;
        if (currentUser == null || otherUser == null || currentUser.ObjectId == otherUser.ObjectId)
            return null;

        var otherUserQuery = new ParseQuery<UserModelOnline>(ParseClient.Instance)
            .WhereEqualTo("objectId", otherUser.ObjectId);

        var conversationQuery = new ParseQuery<ChatConversation>(ParseClient.Instance)
            .WhereEqualTo("isDirectMessage", true)
            .WhereMatchesQuery("Participants", otherUserQuery)
            .Include("Participants");

        var potentialConversations = await conversationQuery.FindAsync();

        foreach (var conv in potentialConversations)
        {
            var relation = conv.GetRelation<UserModelOnline>("Participants").Query;
            var participants = await relation.FindAsync();

            if (participants.Count() == 2 && participants.Any(p => p.ObjectId == currentUser.ObjectId))
            {

                SelectedConversation = conv;

                NewMessageText="joining chat "+DateTime.UtcNow;
                await SendMessageCommand.Execute();


                await FindAllMessagesInconvo(conv);
                return conv;
            }
        }

        var newConversation = new ChatConversation
        {
            IsGroupChat = false,
            IsDirectMessage = true,
            Name = $"{currentUser.Username} & {otherUser.Username}"
        };

        var currentUserPointer = ParseClient.Instance.CreateObjectWithoutData<UserModelOnline>(currentUser.ObjectId);
        var otherUserPointer = ParseClient.Instance.CreateObjectWithoutData<UserModelOnline>(otherUser.ObjectId);

        newConversation.Participants.Add(currentUserPointer);
        newConversation.Participants.Add(otherUserPointer);

        var acl = new ParseACL(currentUser) { PublicReadAccess = true, PublicWriteAccess = true };
        acl.SetReadAccess(otherUser.ObjectId, true);
        acl.SetWriteAccess(otherUser.ObjectId, true);
        newConversation.ACL = acl;

        await newConversation.SaveAsync();
        SelectedConversation = newConversation;

        NewMessageText="Convo Started";
        await SendMessageCommand.Execute();




        return newConversation;
    }







    private async Task SetupLiveQueryListeners()
    {
        var currentUser = ParseClient.Instance.CurrentUser;

        if (currentUser == null)
            return;

        LiveClient = new ParseLiveQueryClient();
        var disposables = new CompositeDisposable();
        LiveClient.Start();

        LiveClient.OnError
        .Subscribe(ex => Debug.WriteLine($"[LiveQuery Error]: {ex.Message}"));

        // You can still listen to these for logging if you wish.
        LiveClient.OnSubscribed
            .Subscribe(e =>
            {
                Debug.WriteLine($"Successfully subscribed with ID: {e.requestId}");
            });


        LiveClient.OnDisconnected
            .Do(info =>
                Debug.WriteLine($"Server disconnected.{info.Reason}"))
            .Subscribe();


        LiveClient.OnSubscribed
            .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
            .Subscribe();

        var currentUserPointer = ParseClient.Instance.CurrentUser;

        LiveClient.OnConnectionStateChanged
      .ObserveOn(RxApp.MainThreadScheduler) // Best practice: ensure UI updates are on the main thread
      .Subscribe(state =>
      {
          Debug.WriteLine($"[LiveQuery Status]: Connection state is now {state}");
          IsConnected = state == LiveQueryConnectionState.Connected;
      });


        // 3. Messages in those conversations
        var messageQuery = new ParseQuery<ChatMessage>(ParseClient.Instance)
           ;
        var messageSub = LiveClient.Subscribe(messageQuery)
            ;
        messageSub.On(Subscription.Event.Create, async msg =>
        {
            var messageType = msg.MessageType;
            var currentUserId = ParseUser.CurrentUser.ObjectId;
            
            // Use a switch to handle different commands
            switch (messageType)
            {
                case "SessionTransfer":
                    if ((msg.UserDeviceVersion == DeviceInfo.Current.VersionString) && (msg.UserDevicePlatform == DeviceInfo.Current.Platform.ToString()))
                    {
                        return;
                    }

                    NewMessageText = "Session Transfer";

                    await SendMessageInternalAsync();

                    var currentSong = viewModel.CurrentPlayingSongView;
                    var currentSongBytes = File.ReadAllBytes(currentSong.FilePath);

                    ParseFile currentSongFile = new ParseFile(currentSong.Title, currentSongBytes);
                    await currentSongFile.SaveAsync(ParseClient.Instance);

                 



                    break;
                case "PlaylistReceived":
                    Debug.WriteLine("Playlist received notification!");

                    // 1. Get the ParseFile object from the message
                    var playlistFile = msg.Get<ParseFile>("playlistFile");
                    if (playlistFile != null && playlistFile.Url != null)
                    {

                        using (var httpClient = new HttpClient())
                        {
                            var jsonContent = await httpClient.GetStringAsync(playlistFile.Url);


                            var receivedSongs = JsonSerializer.Deserialize<List<DimmerSharedSong>>(jsonContent);
                            if (receivedSongs is null)
                                return;

                            var MyListOfSongsOnDeviceA = viewModel._mapper.Map<ObservableCollection<SongModelView>>((receivedSongs));
                            Debug.WriteLine($"Successfully downloaded and parsed {receivedSongs.Count} songs.");
                        }
                    }
                    break;

                case "PlaySong":

                    Debug.WriteLine("Playy");
                    break;

                default:
                    _messagesCache.AddOrUpdate(msg); // It's a regular chat message
                    break;
            }
        });
        messageSub.On(Subscription.Event.Update, (msg, _) =>
        {
            _messagesCache.AddOrUpdate(msg);
        });
        messageSub.On(Subscription.Event.Delete, msg =>
        {
            _messagesCache.Remove(msg);
        });



        NewMessageText="Joined General Channel";
        NewMessageText = DimmerPlaybackState.ChatJoinedGeneralChannel.ToString();
        await SendMessageInternalAsync();
        return;
    }

    public async Task GetSongs()
    {

        NewMessageText="Joined General Channel";
        NewMessageText = DimmerPlaybackState.ChatJoinedGeneralChannel.ToString();
        await SendMessageInternalAsync();
        return;
    }



    public async Task SendPlaylistToDeviceA(List<SongModelView> songs, string deviceAUserId)
    {
        var shareSongs = songs.Select(song =>
        {

            // Convert your DimmerSharedSong to a simpler object if needed
            return new
        DimmerSharedSong()
            {
                Title=                song.Title,
                ArtistName=song.ArtistName,
                AlbumName=song.AlbumName,
                GenreName=song.GenreName,
                IsFavorite=song.IsFavorite,
                IsPlaying=song.IsPlaying,
            };



        }).ToList();

        var parameters = new Dictionary<string, object>
    {
        // NOTE: Your SongModel must be serializable to basic types.
        // If it's complex, convert it to a simpler object first.
        { "songs", songs },
        { "recipientId", deviceAUserId }
    };

        try
        {
            // Call the cloud function and wait for its response
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<Dictionary<string, object>>("transferPlaylist", parameters);
            Debug.WriteLine($"Cloud function result: {result["message"]}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to transfer playlist: {ex.Message}");
        }
    }

    public async Task EndLiveQueries()
    {
        if (LiveClient != null)
        {
            await LiveClient.RemoveAllSubscriptions();
            await LiveClient.DisconnectAsync();
            await LiveClient.DisposeAsync();
            LiveClient = null;
        }
        AllMessages?.Clear();
        AllMessages = null;
    }


    private async Task<string> GetUniqueSenderIdAsync()
    {
        var currentUser = _authService.CurrentUser;
        if (currentUser?.ObjectId != null)
        {
            return currentUser.ObjectId;
        }


        try
        {

            var installation = await ParseClient.Instance.CurrentInstallationController.GetAsync(ParseClient.Instance);



            if (installation.ObjectId is null)
            {


                await installation.SaveAsync();
            }


            return installation.ObjectId;
        }
        catch (Exception ex)
        {

            Console.WriteLine($"Critical Error: Could not retrieve or save installation to get a unique ID. {ex.Message}");

            return "anonymous-error";
        }
    }


    private async Task SendMessageInternalAsync()
    {
        // --- Your existing validation is perfect ---
        if (string.IsNullOrWhiteSpace(NewMessageText))
        {
            return;
        }


        var message = new ChatMessage
        {
            Text = NewMessageText,
            MessageType = LatestMessageType,
            UserSenderId = await GetUniqueSenderIdAsync(),
            UserDevicePlatform = DeviceInfo.Platform.ToString(),
            UserDeviceIdiom = DeviceInfo.Idiom.ToString(),
            UserDeviceVersion = DeviceInfo.VersionString,
        };
        message.UserName = CurrentUser?.Username ?? $"Listener #{message.UserSenderId}";


        DimmerSharedSong currentSong = new()
        {
            Title = viewModel.CurrentPlayingSongView.Title,
            ArtistName = viewModel.CurrentPlayingSongView.OtherArtistsName,
            AlbumName = viewModel.CurrentPlayingSongView.AlbumName,
            GenreName = viewModel.CurrentPlayingSongView.GenreName,
            IsFavorite = viewModel.CurrentPlayingSongView.IsFavorite,
            IsPlaying = viewModel.IsPlaying,
            SharedPositionInSeconds = viewModel.CurrentTrackPositionSeconds,

        };
        await currentSong.SaveAsync();


        var currSongPointer = ParseClient.Instance.CreateObjectWithoutData<DimmerSharedSong>(currentSong.ObjectId);
        message.SharedSong = currSongPointer;


        await message.SaveAsync();

        NewMessageText = string.Empty;
    }
    private async Task EditMessageInternalAsync(ChatMessage message)
    {
        // Assume you got new text from a popup...
        string newText = NewMessageText;
        message.MessageType = "EditedMessage";
        message.Text = newText;
        await message.SaveAsync();
        NewMessageText=string.Empty;

    }

    private Task DeleteMessageInternalAsync(ChatMessage message)
    {
        // A "soft delete" is often better than a hard delete.
        message.IsDeleted = true;
        return message.SaveAsync();
    }

    private async Task ReactToMessageInternalAsync(ChatMessage message, string emoji)
    {
        var userId = await GetUniqueSenderIdAsync();
        message.Increment("reactionCount"); // Example field
        message.AddUniqueToList($"reactions_{emoji}", userId); // e.g., reactions_❤️
        await message.SaveAsync();
    }


    ParseLiveQueryClient? LiveClient;
    [RelayCommand]
    async Task SetupLiveQueries()
    {
        await SetupLiveQueryListeners();

        //if (LiveClient == null)
        //{
        //    Debug.WriteLine("[LiveQuery Error]: LiveQuery client is not initialized.");
        //    return;
        //}
        //await SetupLiveQuery();
    }

    #endregion

    // method to set up user session transfer
    // user subsribes LQ to be able to transfer data between devices


    //method to send commands to the selected device e.g GetALlSongs, GetAllArtists, GetAllAlbums, GetAllGenres, GetAllPlaylists, GetAllSongsInPlaylist, etc.
    //play song,pause song, next song, previous song, etc.


    //essentially, we are using a chatmessage object to send to parse server, which will then be sent to the selected device.
    // user saves the chat messageobject with messagetype PlayCommand, NextTrackCommand, PreviousTrackCommand, etc.
    // parse server will save messageobject and ensure it's only ACL to selected User.

    // i need a cloud method to get list of songmodelview, convert to chatmessage, and send to the selected device.
    //send to parse server, which will then send save 1 parse chatmessage object pushPlaylist to target device.
    //so the lq will let receiver call another parse cloud function to get the playlist.




















    private async Task<AuthResult> SendFriendRequestInternalAsync(string username)
    {
        CurrentUser = _authService.CurrentUser;

        var recipientQuery = new ParseQuery<UserModelOnline>(ParseClient.Instance);
        var recipient = await recipientQuery.WhereEqualTo("userUserModelOnline?e", username).FirstOrDefaultAsync();
        var currUsr = ParseClient.Instance.CreateObjectWithoutData<UserModelOnline>(CurrentUser.ObjectId);
        if (recipient == null)
            return AuthResult.Failure("User not found.");

        var request = new FriendRequest();
        request.Recipient = recipient;
        request.Sender = currUsr;
        request.Status = "pending";

        await request.SaveAsync();
        return AuthResult.Success();
    }


    private async Task AcceptFriendRequestInternalAsync(FriendRequest requestObjectId)
    {
        // This MUST be a Cloud Code function for security.
        // The parameters would be the objectId of the request.
        var parameters = new Dictionary<string, object> { { "requestId", requestObjectId.ObjectId } };
        await ParseClient.Instance.CallCloudCodeFunctionAsync<FriendRequest>("acceptFriendRequest", parameters);
    }

    private Task RejectFriendRequestInternalAsync(FriendRequest request) => request.DeleteAsync();

    public void Dispose()
    {
        // You should also dispose your commands if they have disposables.
        (PlayPauseCommand as IDisposable)?.Dispose();
        (NextTrackCommand as IDisposable)?.Dispose();
        (PreviousTrackCommand as IDisposable)?.Dispose();
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