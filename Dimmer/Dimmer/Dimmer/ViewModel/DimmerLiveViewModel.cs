using Dimmer.DimmerLive.Interfaces.Services;

using DynamicData;
using DynamicData.Binding;

using Parse.LiveQuery;

using ReactiveUI;

using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;
// Inherits from ReactiveObject for FRP capabilities.
public partial class DimmerLiveViewModel :ObservableObject, IReactiveObject, IActivatableViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly IDeviceConnectivityService _deviceConnectivityService;
    private readonly ReadOnlyObservableCollection<DeviceState> _availablePlayers;
    private readonly CompositeDisposable _disposables = new();
    // --- PUBLIC PROPERTIES FOR UI BINDING ---

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


    IAuthenticationService authService)
    {
        _authService = authService;
        _deviceConnectivityService = deviceConnectivityService;

        // --- DECLARATIVE SETUP IN THE CONSTRUCTOR ---

        // 1. Bind the list of available players from the service to our public property.
        _deviceConnectivityService.AvailablePlayers
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _availablePlayers)
            .Subscribe(); // This subscription is permanent for the life of the ViewModel.

        // 2. Define the conditions under which the remote control commands can be executed.
        //    The command can only execute when `ControlledDeviceState` is not null.
        var canExecuteRemoteCommand = this.WhenAnyValue(vm => vm.ControlledDeviceState)
                                          .Select(state => state != null);

        // 3. Create the ReactiveCommands.
        NextTrackCommand = ReactiveCommand.CreateFromTask(
            () => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "NEXT"),
            canExecuteRemoteCommand
        );
        PreviousTrackCommand = ReactiveCommand.CreateFromTask(
            () => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "PREVIOUS"),
            canExecuteRemoteCommand
        );
        PlayPauseCommand = ReactiveCommand.CreateFromTask(
            () => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "PLAY_PAUSE"),
            canExecuteRemoteCommand
        );

        // 4. This is the core logic for "State Mirroring".
        //    We create an observable stream that reacts whenever the user selects a different device.
        var controlledDeviceStream = this.WhenAnyValue(vm => vm.ControlledDeviceState);

        // 5. We use the `Switch` operator to manage the Live Query subscription.
        //    `Switch` is incredibly powerful: it automatically unsubscribes from the old device's
        //    update stream and subscribes to the new one whenever `controlledDeviceStream` changes.
        var stateUpdateStream = controlledDeviceStream
            .Select(device =>
            {
                if (device == null)
                {
                    // If no device is selected, return an empty stream.
                    return Observable.Empty<DeviceState>();
                }
                // For the selected device, create a new observable stream from its Live Query updates.
                return Observable.Create<DeviceState>(async observer =>
                {
                    var query = new ParseQuery<DeviceState>(ParseClient.Instance).WhereEqualTo("objectId", device.ObjectId);
                    var sub = await _deviceConnectivityService.LiveQueryClient.SubscribeAsync(query);

                    var updateDisposable = sub.On(Subscription.Event.Update, (updatedState, original) =>
                    {
                        observer.OnNext(updatedState);
                    });
                    // Return a disposable that will automatically unsubscribe when Switch moves to the next device.
                    return new CompositeDisposable(updateDisposable, Disposable.Create(() => sub.UnsubscribeNow()));
                });
            })
            .Switch(); // This is the magic that manages the subscriptions automatically.

        // 6. Bind the output of our state update stream to our public properties.
        _mirroredPlaybackState = stateUpdateStream
            .Select(state => state.PlaybackState)
            .StartWith("Not Connected") // Provide an initial value
            .ToProperty(this, vm => vm.MirroredPlaybackState);

        _mirroredCurrentSongId = stateUpdateStream
            .Select(state => state.CurrentSongId)
            .StartWith(string.Empty)
            .ToProperty(this, vm => vm.MirroredCurrentSongId);

        // --- LIFECYCLE MANAGEMENT using IActivatableViewModel ---

        this.WhenActivated(disposables =>
        {
            // This code runs when the View is shown.
            _deviceConnectivityService.InitializeAsync().Wait(); // Use .Wait() only if necessary, better to make activator async.
            _deviceConnectivityService.StartListeners();

            // This code runs when the View is hidden.
            Disposable.Create(() => _deviceConnectivityService.StopListeners()).DisposeWith(disposables);
        });





        var friendsSort = SortExpressionComparer<UserModelOnline>.Ascending(u => u.Username);
        _friendsCache.Connect().Sort(friendsSort).ObserveOn(RxApp.MainThreadScheduler).Bind(out _friends).Subscribe().DisposeWith(_disposables);

        var requestSort = SortExpressionComparer<FriendRequest>.Descending(r => r.CreatedAt);
        _friendRequestsCache.Connect().Sort(requestSort).ObserveOn(RxApp.MainThreadScheduler).Bind(out _friendRequests).Subscribe().DisposeWith(_disposables);

        var convoSort = SortExpressionComparer<ChatConversation>.Descending(c => c.LastMessageTimestamp);
        _conversationsCache.Connect().Sort(convoSort).ObserveOn(RxApp.MainThreadScheduler).Bind(out _conversations).Subscribe().DisposeWith(_disposables);

        this.WhenAnyValue(vm => vm.SelectedConversation)
                  .Select(selectedConvo => _messagesCache.Connect().Filter(msg => selectedConvo != null && msg.Conversation?.ObjectId == selectedConvo.ObjectId))
                  .Switch()
                  .Sort(SortExpressionComparer<ChatMessage>.Ascending(m => m.CreatedAt))
                  .ObserveOn(RxApp.MainThreadScheduler)
                  .Bind(out _messagesForSelectedConversation)
                  .Subscribe()
                  .DisposeWith(_disposables);

        #region --- Command Implementation ---


        #region --- Command Implementation ---

        NextTrackCommand = ReactiveCommand.CreateFromTask(() => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "NEXT"), canExecuteRemoteCommand);
        PreviousTrackCommand = ReactiveCommand.CreateFromTask(() => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "PREVIOUS"), canExecuteRemoteCommand);
        PlayPauseCommand = ReactiveCommand.CreateFromTask(() => _deviceConnectivityService.SendCommandAsync(ControlledDeviceState!.DeviceId, "PLAY_PAUSE"), canExecuteRemoteCommand);

        SendFriendRequestCommand = ReactiveCommand.CreateFromTask<string, AuthResult>(SendFriendRequestInternalAsync);
        // FIX: Correct return type for commands that return Task. It should be Unit.
        AcceptFriendRequestCommand = ReactiveCommand.CreateFromTask<FriendRequest>(AcceptFriendRequestInternalAsync);
        RejectFriendRequestCommand = ReactiveCommand.CreateFromTask<FriendRequest>(RejectFriendRequestInternalAsync);

        var canSendMessage = this.WhenAnyValue(vm => vm.NewMessageText, vm => vm.SelectedConversation, (text, chat) => !string.IsNullOrWhiteSpace(text) && chat != null);
        SendMessageCommand = ReactiveCommand.CreateFromTask(() => SendMessageInternalAsync(NewMessageText), canSendMessage);

        EditMessageCommand = ReactiveCommand.CreateFromTask<ChatMessage>(EditMessageInternalAsync);
        DeleteMessageCommand = ReactiveCommand.CreateFromTask<ChatMessage>(DeleteMessageInternalAsync);
        ReactToMessageCommand = ReactiveCommand.CreateFromTask<ChatMessage>(message => ReactToMessageInternalAsync(message, "❤️"));

        #endregion

        #region --- Activation Logic ---

        this.WhenActivated(d =>
        {
            _authService.CurrentUser
                .Select(user => user != null)
                .DistinctUntilChanged()
                .Subscribe(isLoggedIn =>
                {
                    IsSocialHubActive = isLoggedIn;
                    if (isLoggedIn)
                    {
                        SetupLiveQueryListeners().DisposeWith(d);
                    }
                    else
                    {
                        // User logged out, clear all local social data.
                        _friendsCache.Clear();
                        _friendRequestsCache.Clear();
                        _conversationsCache.Clear();
                        _messagesCache.Clear();
                    }
                })
                .DisposeWith(d);
        });

        #endregion
    }
    private async Task<IDisposable> SetupLiveQueryListeners()
    {
        var currentUser = ParseClient.Instance.CurrentUser;

        if (currentUser == null)
            return Disposable.Empty;

        var client = _deviceConnectivityService.LiveQueryClient;
        var disposables = new CompositeDisposable();


        var currentUserPointer = ParseClient.Instance.CreateObjectWithoutData<ParseUser>(currentUser.ObjectId);


        // 1. Friend Requests
        var requestQuery = new ParseQuery<FriendRequest>(ParseClient.Instance).WhereEqualTo("recipient", currentUser);
        var requestSub = await client.SubscribeAsync(requestQuery);
        requestSub.On(Subscription.Event.Create, req => _friendRequestsCache.AddOrUpdate(req));
        requestSub.On(Subscription.Event.Delete, req => _friendRequestsCache.Remove(req));

        // 2. Conversations
        var convoQuery = new ParseQuery<ChatConversation>(ParseClient.Instance).WhereEqualTo("participants", currentUser);
        var convoSub = client.SubscribeAsync(convoQuery).Result;
        convoSub.On(Subscription.Event.Create, chat => _conversationsCache.AddOrUpdate(chat));
        convoSub.On(Subscription.Event.Update, (chat, _) => _conversationsCache.AddOrUpdate(chat));

        // 3. Messages in those conversations
        var messageQuery = new ParseQuery<ChatMessage>(ParseClient.Instance).WhereMatchesQuery("conversation", convoQuery);
        var messageSub = client.SubscribeAsync(messageQuery).Result;
        messageSub.On(Subscription.Event.Create, msg => _messagesCache.AddOrUpdate(msg));
        messageSub.On(Subscription.Event.Update, (msg, _) => _messagesCache.AddOrUpdate(msg));
        messageSub.On(Subscription.Event.Delete, msg => _messagesCache.Remove(msg));

        return disposables;
    }

    #region --- Internal Business Logic ---

    private async Task<AuthResult> SendFriendRequestInternalAsync(string username)
    {
        var recipientQuery = new ParseQuery<UserModelOnline>(ParseClient.Instance).WhereEqualTo("username", username);
        var recipient = await recipientQuery.FirstOrDefaultAsync();
        if (recipient == null)
            return AuthResult.Failure("User not found.");

        var request = new FriendRequest();
        request.Recipient = recipient;
        request.Sender = ParseClient.Instance.CurrentUser;
        request.Status = "pending"; 

        await request.SaveAsync();
        return AuthResult.Success();
    }

    private async Task AcceptFriendRequestInternalAsync(FriendRequest request)
    {
        // This MUST be a Cloud Code function for security.
        // The parameters would be the objectId of the request.
        var parameters = new Dictionary<string, object> { { "requestId", request.ObjectId } };
        await ParseClient.Instance.CallCloudCodeFunctionAsync<FriendRequest>("acceptFriendRequest", parameters);
    }

    private Task RejectFriendRequestInternalAsync(FriendRequest request) => request.DeleteAsync();

    private async Task SendMessageInternalAsync(string text)
    {
        var message = new ChatMessage
        {
            Text = text,
            Sender = ParseClient.Instance.CurrentUser,
            Conversation = SelectedConversation
        };
        NewMessageText = string.Empty;
        await message.SaveAsync();
    }

    private async Task EditMessageInternalAsync(ChatMessage message)
    {
        // Assume you got new text from a popup...
        string newText = "This is the edited text.";
        message.Text = newText;
        await message.SaveAsync();
    }

    private Task DeleteMessageInternalAsync(ChatMessage message)
    {
        // A "soft delete" is often better than a hard delete.
        message.IsDeleted = true;
        return message.SaveAsync();
    }

    private async Task ReactToMessageInternalAsync(ChatMessage message, string emoji)
    {
        var userId = ParseClient.Instance.CurrentUser.ObjectId;
        // This is an atomic operation, perfect for Cloud Code.
        // For client-side, we can use an atomic operation.
        message.Increment("reactionCount"); // Example field
        message.AddUniqueToList($"reactions_{emoji}", userId); // e.g., reactions_❤️
        await message.SaveAsync();
    }

    #endregion



    private readonly SourceCache<UserModelOnline, string> _friendsCache = new(user => user.ObjectId);
    private readonly SourceCache<FriendRequest, string> _friendRequestsCache = new(req => req.ObjectId);
    private readonly SourceCache<ChatConversation, string> _conversationsCache = new(chat => chat.ObjectId);
    private readonly SourceCache<ChatMessage, string> _messagesCache = new(msg => msg.ObjectId);

    private readonly ReadOnlyObservableCollection<UserModelOnline> _friends;
    private readonly ReadOnlyObservableCollection<FriendRequest> _friendRequests;
    private readonly ReadOnlyObservableCollection<ChatConversation> _conversations;
    private readonly ReadOnlyObservableCollection<ChatMessage> _messagesForSelectedConversation;

    public ReadOnlyObservableCollection<UserModelOnline> Friends => _friends;
    public ReadOnlyObservableCollection<FriendRequest> FriendRequests => _friendRequests;
    public ReadOnlyObservableCollection<ChatConversation> Conversations => _conversations;
    public ReadOnlyObservableCollection<ChatMessage> MessagesForSelectedConversation => _messagesForSelectedConversation;

    [ObservableProperty] public partial ChatConversation? SelectedConversation { get; set; }
    [ObservableProperty] public partial string NewMessageText {get;set;}
    [ObservableProperty] public partial bool IsSocialHubActive {get;set;}


    public ReactiveCommand<string, AuthResult> SendFriendRequestCommand { get; }
    public ReactiveCommand<FriendRequest, Unit> AcceptFriendRequestCommand { get; }
    public ReactiveCommand<FriendRequest, Unit> RejectFriendRequestCommand { get; }
    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }
    public ReactiveCommand<ChatMessage, Unit> EditMessageCommand { get; }
    public ReactiveCommand<ChatMessage, Unit> DeleteMessageCommand { get; }
    public ReactiveCommand<ChatMessage, Unit> ReactToMessageCommand { get; } // Assumes a payload from CommandParameter





















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
#endregion