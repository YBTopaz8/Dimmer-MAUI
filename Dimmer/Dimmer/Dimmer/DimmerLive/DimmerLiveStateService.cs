using System.Reactive.Disposables;
using ATL;
using Dimmer.DimmerLive.Models;
using Dimmer.Utilities.FileProcessorUtils;
using Dimmer.Utils;
using Parse.Infrastructure;
using Parse.LiveQuery;

namespace Dimmer.DimmerLive;
public class DimmerLiveStateService : IDimmerLiveStateService
{

    public IObservable<bool> IsLiveQueryConnected => _isLiveQueryConnectedSubject.AsObservable();

    private readonly BehaviorSubject<bool> _isLiveQueryConnectedSubject = new BehaviorSubject<bool>(false);

    private readonly PasswordEncryptionService _encryptionService;

    private ParseLiveQueryClient? liveClient;
    private readonly Dictionary<string, Subscription> _messageSubscriptions = new Dictionary<string, Subscription>();
    private Subscription? _conversationListSubscription;


    public UserModelOnline? UserOnline { get; internal set; }
    public UserModel UserLocalDB { get; internal set; }
    public UserModelView UserLocalView { get; internal set; }

    private readonly CompositeDisposable _serviceSubs = new CompositeDisposable();


    private bool IsConnected = false;



    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<GenreModel> _genreRepo;
    private readonly IRepository<DimmerPlayEvent> _pdlRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<UserModel> _userRepo;
    private readonly IDimmerStateService _state;
    private readonly IMapper mapper;
    readonly CompositeDisposable _subs = new();

    public DimmerLiveStateService(IMapper mapper,

        IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<UserModel> userRepo,
        IRepository<DimmerPlayEvent> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
         IDimmerStateService _state)
    {
        this._userRepo=userRepo;
        this._state=_state;
        this.mapper=mapper;
        _encryptionService = new PasswordEncryptionService();
        UserLocalView ??= BaseAppFlow.CurrentUserView;
    }
    public async Task InitializeAfterLogin(UserModelOnline authenticatedUser)
    {
        UserOnline = authenticatedUser;

        await SetupLiveQueryAsync();
    }

    public void CleanupAfterLogout()
    {
        UserOnline = null;
        _isLiveQueryConnectedSubject.OnNext(false);

        _conversationListSubscription?.UnsubscribeNow();
        _conversationListSubscription = null;

        foreach (var subPair in _messageSubscriptions)
        {
            subPair.Value.UnsubscribeNow(); // Unsubscribe from each message subscription

        }
        _messageSubscriptions.Clear();

//        if (liveClient.Dispose())
//            ;
//        liveClient = null;
//    }
//}
    }


    // In User A's client, after they select a device (e.g., by its objectId from the list)
    public static async Task<bool> SetActiveChatDeviceAsync(string selectedDeviceSessionObjectId)
    {
        if (ParseClient.Instance.CurrentUserController.CurrentUser == null)
            return false;

        var currentDeviceIdentifier = DeviceInfo.Name; // Or a more specific unique ID for the current device

        var parameters = new Dictionary<string, object>
    {
        { "selectedDeviceSessionObjectId", selectedDeviceSessionObjectId }, // The UserDeviceSession objectId they picked
        { "currentDeviceName", DeviceInfo.Name },       // Info about the device making the call
        { "currentDeviceId", DeviceStaticUtils.GetCurrentDeviceId() }, // Your unique ID for *this physical device*
        { "currentDeviceIdiom", DeviceInfo.Current.Idiom.ToString() },
        { "currentDeviceOSVersion", DeviceInfo.Current.VersionString }
        // No need to send "isActive: true" here, the cloud code will manage it.
    };

        try
        {
            // This cloud function will handle updating/creating the session
            // and deactivating other sessions for the same user.
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("setActiveChatDevice", parameters);
            Debug.WriteLine($"Set active chat device result: {result?.TryGetValue("message", out var objj)}");
            return result != null && result.TryGetValue("success", out var successVal) && (bool)successVal;
        }
        catch (ParseFailureException ex)
        {
            Debug.WriteLine($"Error setting active chat device: {ex.Code} - {ex.Message}");
            return false;
        }
    }



    public IObservable<IEnumerable<ChatConversation>> ObserveUserConversations()
    {
        throw new NotImplementedException();
    }

    public IObservable<IEnumerable<ChatMessage>> ObserveMessagesForConversation(string conversationId)
    {
        throw new NotImplementedException();
    }



    //public async Task<ChatConversation?> GetOrCreateConversationWithUserAsync(UserModelOnline otherUser)
    //{
    //    if (UserOnline == null || otherUser == null || UserOnline.ObjectId == otherUser.ObjectId)
    //        return null;

    //    // Correct way to create pointers for the query
    //    var currentUserPointer = ParseClient.Instance.CreateObjectWithoutData<UserModelOnline>(UserOnline.ObjectId);
    //    var otherUserPointer = ParseClient.Instance.CreateObjectWithoutData<UserModelOnline>(otherUser.ObjectId);



    //    var query =  ParseClient.Instance.GetQuery<ChatConversation>()
    //        .WhereContainsAll(nameof(ChatConversation.Participants), new[] { UserOnline.ObjectId, otherUser.ObjectId }
    //        .Select(id => ParseClient.Instance.CreateObjectWithoutData<UserModelOnline>(id)))            

    //        .WhereEqualTo(nameof(ChatConversation.IsGroupChat), false)
    //        .Include(nameof(ChatConversation.Participants)); // Optional: Include participants if you need their data immediately

    //    try
    //    {
    //        var existing = await query.FirstOrDefaultAsync();
    //        if (existing != null)
    //            return existing;

    //        var newConvo = new ChatConversation { IsGroupChat = false, LastMessageTimestamp = DateTime.UtcNow };
    //        var participantsRel = newConvo.GetRelation<UserModelOnline>(nameof(ChatConversation.Participants));
    //        participantsRel.Add(UserOnline);
    //        participantsRel.Add(otherUser);
    //        var acl = new ParseACL();
    //        acl.SetReadAccess(UserOnline, true);
    //        acl.SetWriteAccess(UserOnline, true);
    //        acl.SetReadAccess(otherUser, true);
    //        acl.SetWriteAccess(otherUser, true);
    //        newConvo.ACL = acl;
    //        await newConvo.SaveAsync();
    //        return newConvo;
    //    }
    //    catch (Exception ex) { Debug.WriteLine($"[GetOrCreateConversation_ERROR] {ex.Message}"); return null; }
    //}

    public async Task<ChatMessage?> SendTextMessageAsync(ChatConversation conversation, string text)
    {
        ChatMessage msg = new()
        {
            Sender = UserOnline,
            Text = text,
            Conversation = conversation,

        };
        msg.MessageType = "Text";
        await msg.SaveAsync();

        return msg;
    }

    public Task<ChatMessage?> ShareSongInChatAsync(string conversationId, DimmerSharedSong songToShare)
    {
        throw new NotImplementedException();
    }

    public Task MarkConversationAsReadAsync(string conversationId)
    {
        throw new NotImplementedException();
    }



    public void Dispose()
    {
        _subs.Dispose();
    }

    public Task FullySyncUser(string userEmail)
    {
        IReadOnlyCollection<SongModel>? allSongs = _songRepo.GetAll();
        IReadOnlyCollection<GenreModel>? allGenres = _genreRepo.GetAll();
        IReadOnlyCollection<ArtistModel>? allArtists = _artistRepo.GetAll();
        IReadOnlyCollection<PlaylistModel>? allPlaylists = _playlistRepo.GetAll();
        IReadOnlyCollection<AlbumModel>? allAlbums = _albumRepo.GetAll();
        // i'll use parse cloud code to call a fxn and pass
        // useremail, allSongs, allGenres, allPlaylists, allArtists, allAlbums
        // allLinks etc. when done, sen

        return Task.CompletedTask;
    }
    public void TransferUserCurrentDevice(string userId, string originalDeviceId, string newDeviceId)
    {
        // first open a live query connection,
        // listening to UserDeviceSession
        // then send a "Pinged at {datetime.now :Dd/MM/yyyy HH:mm:ss}" 
        // user
        //parse cloud code to transfer user device
        //i will pass the userId, originalDeviceId, newDeviceId
        // also pass the currently playing song and position
    }

    public void GetAllConnectedDeviced(UserModelOnline currentUser)
    {

        // parse cloud code

        // i will return a list of devices from devicesession etc etc

    }

    async Task SetupLiveQueryAsync()
    {
        try
        {
            liveClient ??= new ParseLiveQueryClient();
            var query = ParseClient.Instance.GetQuery<UserDeviceSession>();
            var queryConvo = ParseClient.Instance.GetQuery<ChatConversation>()
             .WhereEqualTo(nameof(ChatConversation.Participants), UserOnline) // Key filter
             .Include(nameof(ChatConversation.LastMessage)) // Important for updates
             .Include($"{nameof(ChatConversation.LastMessage)}.{nameof(ChatMessage.Sender)}");

            var queryMsgs = ParseClient.Instance.GetQuery<ChatMessage>();
            //query.WhereEqualTo("isActive", true);
            //WhereEqualTo("userOwner", UserOnline)                
            //.Include("userOwner");

            //Subscription<UserDeviceSession>? subscription = new();
            var subscription = await liveClient!.SubscribeAsync(query);
            var convoSub = await liveClient.SubscribeAsync(queryConvo, "MyConversations");
            var msgSub = await liveClient.SubscribeAsync(queryMsgs, "MyMessages");

            await liveClient.ConnectIfNeededAsync();
            int retryDelaySeconds = 5;
            int maxRetries = 10;

            liveClient.OnConnected
                .Do(_ => Debug.WriteLine("LiveQuery connected."))
                .RetryWhen(errors =>
                    errors
                        .Zip(Observable.Range(1, maxRetries), (error, attempt) => (error, attempt))
                        .SelectMany(async tuple =>
                        {
                            if (tuple.attempt > maxRetries)
                            {
                                Debug.WriteLine($"Max retries reached. Error: {tuple.error.Message}");
                                return Observable.Throw<Exception>(tuple.error); // Explicit type here
                            }
                            IsConnected = false;
                            Debug.WriteLine($"Retry attempt {tuple.attempt} after {retryDelaySeconds} seconds...");

                            // Explicit reconnect call before retry delay
                            await liveClient.ConnectIfNeededAsync(); // revive app!

                            return Observable.Timer(TimeSpan.FromSeconds(retryDelaySeconds)).Select(_ => tuple.error); // Maintain compatible type
                        })
                )
                .Subscribe(
                    _ =>
                    {
                        IsConnected=true;
                        Debug.WriteLine("Reconnected successfully.");
                    },
                    ex => Debug.WriteLine($"Failed to reconnect: {ex.Message}")
                );

            liveClient.OnObjectEvent

            .Subscribe(e =>
            {
                Debug.WriteLine(e.evt);
                ProcessEvent(e);
            });


            // Combine other potential streams
            Observable.CombineLatest(
                liveClient.OnConnected.Select(_ => "Connected"),
                liveClient.OnDisconnected.Select(_ => "Disconnected"),
                (connected, disconnected) => $"Status: {connected}, {disconnected}"
            )
            .Throttle(TimeSpan.FromSeconds(1)) // Aggregate status changes
            .Subscribe(status => Debug.WriteLine(status));


            await CloudCodeToSetSession();


        }
        catch (Exception ex)
        {
            Debug.WriteLine("SetupLiveQueryAsync Error: " + ex.Message);
        }
    }
    public static async Task GetMyDeviceSessionsAsync()
    {
        // Assuming ParseUser.CurrentUser is valid
        if (ParseClient.Instance.CurrentUserController.CurrentUser == null)
            return;

        var parameters = new Dictionary<string, object>(); // No specific params needed, user is implicit
        try
        {
            // We expect a list of objects that can be deserialized into a POCO
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<List<object>>("getMyDeviceSessions", parameters);

            // Deserialize the generic list of objects (dictionaries) into specific POCOs
            var deviceSessions = new List<UserDeviceSession>();
            if (result != null)
            {
                foreach (var item in result)
                {
                    if (item is IDictionary<string, object> dict)
                    {
                        Debug.WriteLine(dict.Keys);
                        Debug.WriteLine(dict.Values);
                    }
                }
            }
            return;
        }
        catch (ParseFailureException ex)
        {
            Debug.WriteLine($"Error getting my device sessions: {ex.Code} - {ex.Message}");
            return;
        }
    }
    async Task CloudCodeToSetSession()
    {
        // Gather device information
        // Using DeviceInfo.Name as the primary identifier for the session as per your cloud code's query
        var deviceName = DeviceInfo.Name; // This is what your cloud code queries on primarily

        // While deviceId might not be the primary query key in your cloud code,
        // it's still good practice to generate and send it for uniqueness if a deviceName isn't unique enough
        // or if you might change the cloud code later.
        var deviceId = DeviceInfo.Current.Idiom == DeviceIdiom.Desktop ?
                       (DeviceInfo.Current.Name + "_" + DeviceInfo.Current.Model + "_" + System.Net.Dns.GetHostName()).Replace(" ", "_") : // Make it more filesystem/DB friendly
                       DeviceInfo.Current.Platform.ToString();

        var deviceIdiom = DeviceInfo.Current.Idiom.ToString();
        var deviceOSVersion = DeviceInfo.Current.VersionString;
        UserDeviceSession devSess = new();
        devSess.DeviceIdiom = deviceIdiom;
        devSess.DeviceName = deviceName;
        devSess.DeviceId = deviceId;
        devSess.DeviceOSVersion = deviceOSVersion;
        devSess.UserOwner = UserOnline;
        devSess.IsActive = true;

        var parameters = new Dictionary<string, object>
            {
                // userOwner will be request.user in Cloud Code due to authenticated call
                { "deviceId", deviceId },             // Send it, even if not primary query key in current cloud code
                { "deviceName", deviceName },         // This is a key part of your cloud code's query
                { "deviceIdiom", deviceIdiom },
                { "deviceOSVersion", deviceOSVersion },
                { "isActive", true }                  // You're setting the session to active
                // sessionStartTime will be set by the server in your cloud code
            };

   
    }


    Task<ObservableCollection<ChatMessage>> OpenConversation(string convoId)
    {


        // open the conversation
        // i will use the chat service to open the conversation
        // and pass the convoId to it
        return null;
    }


    public void RequestSongFromDifferentDevice(string userId, string songId, string deviceId)
    {
        // use a parse cloud code, send
    }

    async Task SetupLiveQueryDimmerSharedSongAsync()
    {
        try
        {
            liveClient ??= new ParseLiveQueryClient();
            var query = ParseClient.Instance.GetQuery<DimmerSharedSong>();


            //Subscription<UserDeviceSession>? subscription = new();
            var subscription = await liveClient!.SubscribeAsync(query, "SharedSongsSubs");

            await liveClient.ConnectIfNeededAsync();
            int retryDelaySeconds = 5;
            int maxRetries = 10;

            liveClient.OnConnected
                .Do(_ => Debug.WriteLine("LiveQuery connected."))
                .RetryWhen(errors =>
                    errors
                        .Zip(Observable.Range(1, maxRetries), (error, attempt) => (error, attempt))
                        .SelectMany(async tuple =>
                        {
                            if (tuple.attempt > maxRetries)
                            {
                                Debug.WriteLine($"Max retries reached. Error: {tuple.error.Message}");
                                return Observable.Throw<Exception>(tuple.error); // Explicit type here
                            }
                            IsConnected = false;
                            Debug.WriteLine($"Retry attempt {tuple.attempt} after {retryDelaySeconds} seconds...");

                            // Explicit reconnect call before retry delay
                            await liveClient.ConnectIfNeededAsync(); // revive app!

                            return Observable.Timer(TimeSpan.FromSeconds(retryDelaySeconds)).Select(_ => tuple.error); // Maintain compatible type
                        })
                )
                .Subscribe(
                    _ =>
                    {
                        IsConnected=true;
                        Debug.WriteLine("Reconnected successfully.");
                    },
                    ex => Debug.WriteLine($"Failed to reconnect: {ex.Message}")
                );

            liveClient.OnError
                .Do(async ex =>
                {
                    Debug.WriteLine("LQ Error: " + ex.Message);
                    await liveClient.ConnectIfNeededAsync();  // Ensure reconnection on errors
                })
                .OnErrorResumeNext(Observable.Empty<Exception>()) // Prevent breaking the stream
                .Subscribe();

            liveClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();


            liveClient.OnSubscribed
                .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
                .Subscribe();

            liveClient.OnObjectEvent

            .Subscribe(e =>
            {
                Debug.WriteLine(e.evt);
                ProcessEvent(e);
            });


            // Combine other potential streams
            Observable.CombineLatest(
                liveClient.OnConnected.Select(_ => "Connected"),
                liveClient.OnDisconnected.Select(_ => "Disconnected"),
                (connected, disconnected) => $"Status: {connected}, {disconnected}"
            )
            .Throttle(TimeSpan.FromSeconds(1)) // Aggregate status changes
            .Subscribe(status => Debug.WriteLine(status));


            await CloudCodeToSetSession();


        }
        catch (Exception ex)
        {
            Debug.WriteLine("SetupLiveQueryAsync Error: " + ex.Message);
        }
    }



    private string GetMimeTypeForExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return "application/octet-stream";

        // Remove leading dot for comparison, if present
        string ext = extension.StartsWith(".") ? extension.Substring(1) : extension;

        switch (ext.ToLowerInvariant())
        {
            case "mp3":
                return "audio/mpeg";
            case "flac":
                return "audio/flac";
            case "wav":
                return "audio/wav";
            case "m4a":
                return "audio/mp4";
            case "ogg":
                return "audio/ogg";
            case "aac":
                return "audio/aac";
            // Add more audio types as needed
            case "jpg":
            case "jpeg":
                return "image/jpeg";
            case "png":
                return "image/png";
            default:
                return "application/octet-stream";
        }
    }





    public static async Task MarkSessionInactiveAsync()
    {
        var deviceName = DeviceInfo.Name;
        var parameters = new Dictionary<string, object> { { "deviceName", deviceName } };
        try
        {
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("markSessionInactive", parameters);
            if (result != null && result.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                Debug.WriteLine($"Session for {deviceName} marked inactive.");
            }
            else
            {
                object val;
                Debug.WriteLine($"Failed to mark session inactive: {result?.TryGetValue("message", out val)}");
            }
        }
        catch (ParseFailureException ex)
        {
            Debug.WriteLine($"Error marking session inactive: {ex.Code} - {ex.Message}");
        }
    }
    // Call this on logout, app exit, etc.


    void ProcessEvent((Subscription.Event evt, ParseObject obj, Subscription subscription) e)
    {
        string className = e.obj.ClassName; // Get class name from subscription

        Debug.WriteLine($"LQ Event: {e.evt} on Class: {className}, ObjectId: {e.obj.ObjectId}");

        switch (className)
        {
            case "UserDeviceSession":
                var uds = e.obj as UserDeviceSession;
                if (uds != null)
                    ProcessUserDeviceSessionEvent(e.evt, uds);
                break;
            case "ChatConversation":
                var convo = e.obj as ChatConversation;
                if (convo != null)
                    ProcessChatConversationEvent(e.evt, convo);
                break;
            case "ChatMessage":
                var msg = e.obj as ChatMessage;
                if (msg != null)
                    ProcessChatMessageEvent(e.evt, msg);
                break;
            case "DimmerSharedSong":
                var sharedSong = e.obj as DimmerSharedSong;
                if (sharedSong != null)
                    ProcessDimmerSharedSongEvent(e.evt, sharedSong);
                break;
            default:
                Debug.WriteLine($"Unhandled LiveQuery event for class: {className}");
                break;
        }
    }

    // Example handlers (implement these based on your app's needs)
    void ProcessUserDeviceSessionEvent(Subscription.Event evt, UserDeviceSession session) { /* ... */ }
    void ProcessChatConversationEvent(Subscription.Event evt, ChatConversation convo) { /* ... */ }
    void ProcessChatMessageEvent(Subscription.Event evt, ChatMessage message) { /* ... */ }
    void ProcessDimmerSharedSongEvent(Subscription.Event evt, DimmerSharedSong song) { /* ... */ }

    public void DeleteUserLocally(UserModel user)
    {
        _userRepo.Delete(user);
    }

    public async Task DeleteUserOnline(UserModelOnline user)
    {
        await user.DeleteAsync();

    }

    public void SaveUserLocally(UserModelView user)
    {
        var usr = mapper.Map<UserModel>(user);
        _userRepo.AddOrUpdate(usr);
    }

    public async Task SignUpUserAsync(UserModelView user)
    {
        UserLocalDB = mapper.Map<UserModel>(user);
        UserLocalView = user;
        try
        {
            UserOnline = await ParseClient.Instance.SignUpWithAsync(UserLocalDB.UserName, UserLocalDB.UserPassword) as UserModelOnline;
        }
        catch (Exception ex)
        {
            //await Shell.Current.DisplayAlert(AppTitle, ex.Message, "OK");
        }

    }



    public async Task<DimmerSharedSong?> ShareSongOnline(SongModelView song, double positionInSeconds)
    {
        DimmerSharedSong newSong = await ParseClient.Instance.GetQuery<DimmerSharedSong>()
          .WhereEqualTo("artist", song.ArtistName)
          .WhereEqualTo("title", song.Title)

          .FirstOrDefaultAsync();
        if (newSong is null)
        {
            if (song == null || string.IsNullOrEmpty(song.FilePath))
            {
                Debug.WriteLine("Error: Song data or file path is missing.");
                await Shell.Current.DisplayAlert("Error", "Song data or file path is missing.", "OK");
                return null;
            }

            if (!File.Exists(song.FilePath))
            {
                Debug.WriteLine($"Error: File not found at path: {song.FilePath}");
                await Shell.Current.DisplayAlert("Error", $"The song file could not be found at:\n{song.FilePath}\nPlease select it again.", "OK");
                return null;
            }

            var fileNameForParse = Path.GetFileName(song.FilePath);
            Debug.WriteLine($"[SHARE_SONG_INFO] FileName for Parse: {fileNameForParse}"); // LOG THIS

            var actualFileExtension = Path.GetExtension(song.FilePath);
            Debug.WriteLine($"[SHARE_SONG_INFO] Extracted FileExtension: {actualFileExtension}"); // LOG THIS

            var mimeType = GetMimeTypeForExtension(actualFileExtension);
            Debug.WriteLine($"[SHARE_SONG_INFO] Determined MimeType: {mimeType}"); // LOG THIS
            var sanitizedFileNameForParse = $"{Guid.NewGuid()}{actualFileExtension}"; // e.g., "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.flac"

            ParseFile audioFile;

            try
            {
                Debug.WriteLine($"[SHARE_SONG_INFO] Attempting to upload: {fileNameForParse}, MIME: {mimeType}");
                using var audioStream = File.OpenRead(song.FilePath);
                audioFile = new ParseFile(sanitizedFileNameForParse, audioStream, mimeType);
                await audioFile.SaveAsync(ParseClient.Instance);
                Debug.WriteLine($"[SHARE_SONG_INFO] File uploaded successfully: {audioFile.Url}");



                newSong = new DimmerSharedSong();
                newSong.Title = song.Title;
                newSong.Artist = song.ArtistName;
                newSong.Album = song.AlbumName;
                newSong.DurationSeconds = song.DurationInSeconds;
                newSong.AudioFile = audioFile;
                newSong.Uploader = await ParseClient.Instance.GetCurrentUser();
                newSong.SharedPositionInSeconds = positionInSeconds;

                await newSong.SaveAsync();

                await SetupLiveQueryDimmerSharedSongAsync();

                Debug.WriteLine($"[SHARE_SONG_INFO] ParseSong object saved with ID: {newSong.ObjectId}");
                await Share.RequestAsync($"{newSong.Uploader.Username} Shared {song.Title} with you from Dimmer!" +
                    $"\n Download Dimmer and Paste this code {newSong.ObjectId}");

                return newSong;
            }
            catch (ParseFailureException pe)
            {
                Debug.WriteLine($"[SHARE_SONG_ERROR] Parse Exception during ParseSong save: {pe.Code} - {pe.Message}");
                await Shell.Current.DisplayAlert("Save Error", $"Could not save song details: {pe.Message} (Code: {pe.Code})", "OK");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SHARE_SONG_ERROR] Generic Exception during ParseSong save: {ex.Message}");
                await Shell.Current.DisplayAlert("Save Error", $"An unexpected error occurred while saving song details: {ex.Message}", "OK");
                return null;
            }
        }

        else
        {
            Debug.WriteLine($"[SHARE_SONG_INFO] Song already exists in Parse with ID: {newSong.ObjectId}");
            await Shell.Current.DisplayAlert("Info", "This song is already shared.", "OK");
            return newSong;
        }
    }



    public async Task<bool> LoginUserAsync(UserModel usr)
    {
        UserLocalDB = usr;
        try
        {


            UserOnline = await ParseClient.Instance.LogInWithAsync(UserLocalDB.UserName, UserLocalDB.UserPassword) as UserModelOnline;


            if (UserOnline != null && UserLocalDB is not null && !string.IsNullOrEmpty(UserOnline.SessionToken))
            {
                IsConnected=true;

                await SecureStorage.SetAsync("username", UserOnline.Username);

                string? encryptedPassword = await _encryptionService.EncryptAsync(UserLocalDB.UserPassword!);
                if (encryptedPassword != null)
                {
                    await SecureStorage.SetAsync("Password", encryptedPassword); // Store encrypted password
                    UserLocalDB.UserPassword = encryptedPassword; // Update local password with encrypted version
                }
                else
                {
                    // Handle encryption failure - maybe don't store it or log an error
                    Debug.WriteLine("Failed to encrypt password for storage.");
                }
                await SecureStorage.SetAsync("ObjectId", UserOnline.ObjectId);
                await SecureStorage.SetAsync("SessionToken", UserOnline.SessionToken);
                await SecureStorage.SetAsync("Email", UserOnline.Email);

                UserLocalDB.SessionToken = UserOnline.SessionToken;
                _userRepo.AddOrUpdate(UserLocalDB);
                var tcs = new TaskCompletionSource<bool>();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await InitializeAfterLogin(UserOnline);
                        await Shell.Current.DisplayAlert("Success", "User logged in successfully.", "OK");
                        tcs.SetResult(true); // Signal that the UI part is done and successful
                    }
                    catch (Exception ex)
                    {
                        // Handle any exception during DisplayAlert
                        Debug.WriteLine($"Error displaying alert: {ex}");
                        tcs.SetResult(false); // Or tcs.SetException(ex) if you want to propagate
                    }
                });

                return await tcs.Task; // Wait for the alert to be handled and return its outcome

            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Login Error",
                $"{ex.Message}", "OK");

        }
        return false;
    }
    public async Task LogoutUser()
    {
        await ParseClient.Instance.LogOutAsync(CancellationToken.None);
        UserOnline = null;
    }
    public async Task ForgottenPassword()
    {
        await ParseClient.Instance.RequestPasswordResetAsync(UserLocalDB.UserEmail);
        await Shell.Current.DisplayAlert("Success", "Password reset email sent", "OK");
    }
    public async Task<bool> AttemptAutoLoginAsync()
    {

        string? sessionToken = await SecureStorage.GetAsync("SessionToken");
        if (!string.IsNullOrEmpty(sessionToken))
        {
            try
            {
                ParseUser user = await ParseClient.Instance.BecomeAsync(sessionToken);
                UserOnline = user as UserModelOnline;
                UserLocalView = mapper.Map<UserModelView>(user);

                if (UserOnline != null && await UserOnline.IsAuthenticatedAsync())
                {

                    IsConnected = true;
                    await SetupLiveQueryAsync();
                    return true;
                }
                else
                {
                    // Handle case where user is not authenticated
                    Debug.WriteLine("User is not authenticated.");
                    return false;
                }

            }
            catch (Exception ex)
            {
                // Handle any exception during login
                Debug.WriteLine($"Error during auto-login: {ex}");
                return false; // Or handle it as needed
            }
        }
        return false;
    }


    // In User B's client (or any client wanting to see presence)
    // Assume liveQueryClient is initialized
    private Subscription<UserDeviceSession> _presenceSubscription;

    public async Task SubscribeToPresenceUpdatesAsync(IEnumerable<string> userIdsToWatch)
    {
        if (ParseClient.Instance.CurrentUserController.CurrentUser == null)
            return;

        // Create ParseUser pointers for the query
        var userPointers = userIdsToWatch.Select(id => ParseClient.Instance.CreateObjectWithoutData<ParseUser>(id)).ToList();

        // Query for UserDeviceSession objects where:
        // 1. The userOwner is one of the users we're interested in.
        // 2. (Optional, but good for initial state) isActive is true.
        //    LiveQuery will then notify on updates to these, or new ones matching. q
        var presenceQuery = new ParseQuery<UserDeviceSession>(ParseClient.Instance)
            .WhereContainedIn("userOwner", userPointers) // Watch specific users
                                                         // .WhereEqualTo("isActive", true) // Get initially active ones. LQ will update on changes.
            .Include("userOwner"); // To get the username, etc.

        _presenceSubscription = await liveClient!.SubscribeAsync(presenceQuery);

        _presenceSubscription.Events
            .Where(e => e.EventType == Subscription.Event.Update ||
            e.EventType == Subscription.Event.Create)
            .Subscribe(e =>
            {
                var updatedSession = e.Object;
                if (updatedSession != null)
                {
                    // Handle the presence update, e.g., notify UI or update local state
                    Debug.WriteLine($"Presence update for {updatedSession.UserOwner.Username}: {updatedSession.IsActive}");
                }
            });
    }


    public async Task<ChatConversation?> GetOrCreateConversationWithUserAsync(string userId)
    {
        if (UserOnline == null || UserOnline.ObjectId == userId)
        {
            Debug.WriteLine("[GetOrCreateConversation] Invalid parameters: UserOnline or otherUser is null, or self-chat attempt.");
            return null;
        }

        // Create pointers for the query
        var currentUserPointer = ParseClient.Instance.CreateObjectWithoutData<UserModelOnline>(UserOnline.ObjectId);
        var otherUserPointer = ParseClient.Instance.CreateObjectWithoutData<UserModelOnline>(userId);

        // Corrected Query Construction
        // You typically instantiate ParseQuery with the class name if T is not directly the class name string
        // or if T is a registered subclass, it can infer.
        var query = ParseClient.Instance.GetQuery<ChatConversation>() // Assuming "ChatConversation" is the class name on server
            .WhereContainsAll(nameof(ChatConversation.Participants), new[] { currentUserPointer, otherUserPointer })

            .WhereSizeEqualTo(nameof(ChatConversation.Participants), 2) // This applies to the relation field
            .WhereEqualTo(nameof(ChatConversation.IsGroupChat), false)
            .Include((nameof(ChatConversation.Participants))); // Optional: Include if needed immediately

        try
        {
            Debug.WriteLine($"[GetOrCreateConversation] Querying for existing chat between {UserOnline.ObjectId} and {userId}");
            ChatConversation? existing = await query.FirstOrDefaultAsync();
            if (existing != null)
            {
                Debug.WriteLine($"[GetOrCreateConversation] Found existing chat: {existing.ObjectId}");
                return existing;
            }

            Debug.WriteLine($"[GetOrCreateConversation] Creating new chat between {UserOnline.ObjectId} and {userId}");
            var newConvo = new ChatConversation
            {
                IsGroupChat = false,
                LastMessageTimestamp = DateTime.UtcNow
            };

            var participantsRel = newConvo.GetRelation<UserModelOnline>(nameof(ChatConversation.Participants));
            participantsRel.Add(currentUserPointer); // Add the pointer
            participantsRel.Add(otherUserPointer);   // Add the pointer

            var acl = new ParseACL();
            acl.SetReadAccess(UserOnline, true);
            acl.SetWriteAccess(UserOnline, true);
            acl.SetReadAccess(userId, true);
            acl.SetWriteAccess(userId, true);
            newConvo.ACL = acl;

            await newConvo.SaveAsync();
            Debug.WriteLine($"[GetOrCreateConversation] Created new chat: {newConvo.ObjectId}");
            return newConvo;
        }
        catch (ParseFailureException pEx)
        {
            Debug.WriteLine($"[GetOrCreateConversation_ERROR] ParseException: Code={pEx.Code}, Message={pEx.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GetOrCreateConversation_ERROR] Generic Exception: {ex.Message}");
            return null;
        }
    }


    public async Task<DimmerSharedSong?> FetchSharedSongByCodeAsync(string sharedSongCode)
    {
        if (string.IsNullOrWhiteSpace(sharedSongCode))
        {
            Debug.WriteLine("[FETCH_SONG_ERROR] Shared song code is empty.");
            // Consider user-facing alert if appropriate in your UI flow
            return null;
        }

        if (UserOnline == null) // Or ParseUser.CurrentUser
        {
            Debug.WriteLine("[FETCH_SONG_ERROR] User not authenticated.");
            return null;
        }

        try
        {
            var parameters = new Dictionary<string, object>
        {
            { "sharedSongId", sharedSongCode }
        };

            //object sharedSong = await ParseClient.Instance.CallCloudCodeFunctionAsync<object>("getSharedSongDetails", parameters);

            DimmerSharedSong sharedSong = await ParseClient.Instance.CallCloudCodeFunctionAsync<DimmerSharedSong>("getSharedSongDetails", parameters);
            if (sharedSong == null)
            {
                Console.WriteLine("Error: Audio file or its URL is missing from the log entry.");
                // Or use Debug.LogError in Unity, or throw an exception
                return null;
            }

            ParseFile audioFile = sharedSong.AudioFile;
            string songTitle = sharedSong.Title; // Get the title to help name the file

            // Sanitize the song title to create a valid file name
            // Replace invalid characters and ensure it's not too long if necessary
            string fileName = SanitizeFileName(songTitle ?? Path.GetFileNameWithoutExtension(audioFile.Name));
            string extension = Path.GetExtension(audioFile.Name); // Get extension from original ParseFile name
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".mp3"; // Default extension if none found (adjust as needed)
            }
            string fullFileName = $"{fileName}{extension}";
            
                Debug.WriteLine(sharedSong.GetType());
                string savePath;
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDD");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            string localFilePath = Path.Combine(savePath, fullFileName);

            using (HttpClient client = new HttpClient())
                {
                    // Get the file as a byte array
                    byte[] fileBytes = await client.GetByteArrayAsync(audioFile.Url);

                    // Save the byte array to a file
                    await File.WriteAllBytesAsync(localFilePath, fileBytes);

                    Console.WriteLine($"Song '{fullFileName}' downloaded and saved successfully to: {localFilePath}");
                // You can now use localFilePath to play the song or reference it.


              
                }

            Track newFile = new Track(localFilePath);
           


            SongModelView newSong = new SongModelView()
            {
                Title = songTitle,
                FilePath = localFilePath,
                DurationInSeconds = newFile.Duration,
                CoverImagePath = FileCoverImageProcessor.SaveOrGetCoverImageToFilePath(localFilePath),
               
                ArtistName = sharedSong.Artist,
                AlbumName = sharedSong.Album
            };
            _state.SetCurrentSong(mapper.Map<SongModel>(newSong));
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Playing, null));
            _state.SetCurrentLogMsg(new AppLogModel()
                {
                    UserModel = UserLocalView,
                    Log = $"User {UserLocalView.Username} fetched song {sharedSong.Title} with code {sharedSongCode}.",
                    SharedSong = sharedSong
                });
        
            return null;
        }
        catch (ParseFailureException pex)
        {
            Debug.WriteLine($"[FETCH_SONG_ERROR] Parse Error: {pex.Code} - {pex.Message}");
            
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FETCH_SONG_ERROR] Generic Error: {ex.Message}");
            
            return null;
        }
    }

    public async Task<bool> PrepareSessionTransferAsync(SongModelView currentSong, double currentPositionSeconds)
    {
        if (UserOnline == null)
        {
            Debug.WriteLine("[PREPARE_TRANSFER_ERROR] User not logged in.");
            await Shell.Current.DisplayAlert("Error", "You must be logged in to transfer a session.", "OK");
            return false;
        }

        if (currentSong == null || string.IsNullOrEmpty(currentSong.FilePath))
        {
            Debug.WriteLine("[PREPARE_TRANSFER_ERROR] No current song or song file path to transfer.");
            await Shell.Current.DisplayAlert("Error", "No song is currently playing to transfer.", "OK");
            return false;
        }
        DimmerSharedSong? sharedSongToTransfer = await ShareSongOnline(currentSong, currentPositionSeconds);
        if (sharedSongToTransfer == null)
        {
            Debug.WriteLine("[PREPARE_TRANSFER_ERROR] Failed to share song for transfer.");
            await Shell.Current.DisplayAlert("Error", "Failed to prepare song for transfer.", "OK");
            return false;
        }

        // 2. Call the 'prepareSessionTransfer' Cloud Function
        try
        {
            var parameters = new Dictionary<string, object>
        {
            { "sharedSongObjectId", sharedSongToTransfer.ObjectId },
            { "positionInSeconds", currentPositionSeconds },
            { "currentDeviceName", DeviceInfo.Name }, // Device A's name
            { "currentDeviceId", DeviceStaticUtils.GetCurrentDeviceId() } // Device A's unique ID
        };

            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("prepareSessionTransfer", parameters);

            if (result != null && result.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                Debug.WriteLine($"[PREPARE_TRANSFER_SUCCESS] Session transfer prepared for song {sharedSongToTransfer.Title}.");
                // Optionally, mark current device inactive locally or via cloud
                // await MarkSessionInactiveAsync(); // If desired
                await Shell.Current.DisplayAlert("Transfer Ready", "Session transfer is ready. Open Dimmer on your other device.", "OK");
                return true;
            }
            else
            {
                string? message = result?.TryGetValue("message", out var msgObj) == true ? msgObj.ToString() : "Unknown error.";
                Debug.WriteLine($"[PREPARE_TRANSFER_FAIL] Cloud function indicated failure: {message}");
                await Shell.Current.DisplayAlert("Error", $"Could not prepare transfer: {message}", "OK");
                return false;
            }
        }
        catch (ParseFailureException pEx)
        {
            Debug.WriteLine($"[PREPARE_TRANSFER_ERROR] Parse Error: {pEx.Code} - {pEx.Message}");
            await Shell.Current.DisplayAlert("Error", $"Could not prepare transfer: {pEx.Message}", "OK");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PREPARE_TRANSFER_ERROR] Generic Error: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "An unexpected error occurred while preparing the transfer.", "OK");
            return false;
        }
    }

    public async Task<bool> ActivateThisDeviceAndCheckForTransferAsync()
    {
        if (UserOnline == null)
        {
            Debug.WriteLine("[ACTIVATE_DEVICE_ERROR] User not logged in.");
            return false;
        }

        var parameters = new Dictionary<string, object>
    {
        // If the user selected a specific UserDeviceSession object for THIS device from a list
        // (e.g., from GetMyDeviceSessionsAsync), you'd pass its objectId.
        // { "selectedDeviceSessionObjectId", "objectId_of_this_devices_session_if_known_and_selected" },

        // Always send current device's info so cloud code can find/create its session
        { "currentDeviceName", DeviceInfo.Name },
        { "currentDeviceId", DeviceStaticUtils.GetCurrentDeviceId() },
        { "currentDeviceIdiom", DeviceInfo.Current.Idiom.ToString() },
        { "currentDeviceOSVersion", DeviceInfo.Current.VersionString }
    };

        try
        {
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("setActiveChatDevice", parameters);

            if (result != null && result.TryGetValue("success", out var successVal) && (bool)successVal)
            {
                string activeSessionId = result.TryGetValue("activeSessionId", out var idObj) ? idObj.ToString() : "Unknown";
                Debug.WriteLine($"[ACTIVATE_DEVICE_SUCCESS] This device (session: {activeSessionId}) is now active.");

                // Check for pending transfer data
                if (result.TryGetValue("pendingTransfer", out var transferDataObj) &&
                    transferDataObj is IDictionary<string, object> transferDataDict)
                {
                    string? songObjectId = transferDataDict.TryGetValue("songObjectId", out var sIdObj) ? sIdObj.ToString() : null;
                    double positionSeconds = transferDataDict.TryGetValue("positionSeconds", out var posObj) && double.TryParse(posObj.ToString(), out double p) ? p : 0.0;

                    if (!string.IsNullOrEmpty(songObjectId))
                    {
                        Debug.WriteLine($"[ACTIVATE_DEVICE_INFO] Consuming pending transfer for song: {songObjectId} at {positionSeconds}s");
                        DimmerSharedSong? songToPlay = await FetchSharedSongByCodeAsync(songObjectId);
                        if (songToPlay != null && songToPlay.AudioFile != null)
                        {
                            // TODO: Notify your music player service to play this song
                            // MusicPlayerService.PlayRemoteSong(songToPlay.AudioFile.Url, songToPlay.Title, songToPlay.Artist, positionSeconds);
                            Debug.WriteLine($"SUCCESS: Instructing player to play '{songToPlay.Title}' from {songToPlay.AudioFile.Url} at {positionSeconds}s.");
                            await Shell.Current.DisplayAlert("Session Transferred", $"Now playing: {songToPlay.Title}", "OK");
                        }
                        else
                        {
                            Debug.WriteLine($"[ACTIVATE_DEVICE_WARN] Could not fetch details for transferred song ObjectId: {songObjectId}");
                        }
                    }
                }
                return true;
            }
            else
            {
                string? message = result?.TryGetValue("message", out var msgObj) == true ? msgObj.ToString() : "Failed to activate device.";
                Debug.WriteLine($"[ACTIVATE_DEVICE_FAIL] Cloud function indicated failure: {message}");
                await Shell.Current.DisplayAlert("Error", $"Could not activate this device: {message}", "OK");
                return false;
            }
        }
        catch (ParseFailureException pEx)
        {
            Debug.WriteLine($"[ACTIVATE_DEVICE_ERROR] Parse Error: {pEx.Code} - {pEx.Message}");
            await Shell.Current.DisplayAlert("Error", $"Could not activate this device: {pEx.Message}", "OK");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ACTIVATE_DEVICE_ERROR] Generic Error: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "An unexpected error occurred while activating this device.", "OK");
            return false;
        }
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "UntitledSong"; // Default if name is empty
        }
        string invalidChars = new string(Path.GetInvalidFileNameChars());
        string regexPattern = $"[{System.Text.RegularExpressions.Regex.Escape(invalidChars)}]";
        string sanitizedName = System.Text.RegularExpressions.Regex.Replace(name, regexPattern, "_");
        return sanitizedName.Trim('_'); // Remove leading/trailing underscores
    }
}
