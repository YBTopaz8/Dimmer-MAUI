using Dimmer.DimmerLive.Interfaces;
using Dimmer.DimmerLive.Models;
using Dimmer.Utilities.Extensions;
using Dimmer.Utils;
using Parse.Infrastructure;
using Parse.LiveQuery;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive;
public class DimmerLiveStateService : IDimmerLiveStateService
{

    public IObservable<bool> IsLiveQueryConnected => _isLiveQueryConnectedSubject.AsObservable();

    private readonly BehaviorSubject<bool> _isLiveQueryConnectedSubject = new BehaviorSubject<bool>(false);

    private readonly PasswordEncryptionService _encryptionService;

    private ParseLiveQueryClient? liveClient;
    private readonly Dictionary<string, Subscription> _messageSubscriptions = new Dictionary<string, Subscription>();
    private Subscription? _conversationListSubscription;


    public UserModelOnline? UserOnline { get; set; }
    public UserModel UserLocalDB { get; set; }
    public UserModelView UserLocalView { get; set; }

    private readonly CompositeDisposable _serviceSubs = new CompositeDisposable();


    private bool IsConnected = false;



    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<GenreModel> _genreRepo;
    private readonly IRepository<AlbumArtistGenreSongLink> _aagslRepo;
    private readonly IRepository<PlayDateAndCompletionStateSongLink> _pdlRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<UserModel> _userRepo;
    private readonly IDimmerStateService dimmerStateService;
    private readonly IMapper mapper;
    readonly CompositeDisposable _subs = new();

    public DimmerLiveStateService(IMapper mapper,

        IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<UserModel> userRepo,
        IRepository<AlbumArtistGenreSongLink> aagslRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
         IDimmerStateService dimmerStateService)
    {
        this._userRepo=userRepo;
        this.dimmerStateService=dimmerStateService;
        this.mapper=mapper;
        _encryptionService = new PasswordEncryptionService(); 
    }
    public async Task InitializeAfterLogin(UserModelOnline authenticatedUser)
    {
        UserOnline = authenticatedUser;
        await SetupLiveQueryAsync();
    }

    public async Task CleanupAfterLogout()
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

        //if (liveClient.Dispose());
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

    public Task<ChatMessage?> SendTextMessageAsync(string conversationId, string text)
    {
        throw new NotImplementedException();
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
        IReadOnlyCollection<AlbumArtistGenreSongLink>? allAAGSL = _aagslRepo.GetAll();
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
            liveClient= new ParseLiveQueryClient();
            var query = ParseClient.Instance.GetQuery<UserDeviceSession>();
            query.WhereEqualTo("isActive", true).
            WhereEqualTo("isActive", UserOnline)                
                .Include("userOwner");
            
            //Subscription<UserDeviceSession>? subscription = new();
            var subscription =  await liveClient!.SubscribeAsync(query);

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

            int batchSize = 3; // Number of events to release at a time
            TimeSpan throttleTime = TimeSpan.FromMilliseconds(0000);

            liveClient.OnObjectEvent
            .Where(e => e.subscription == subscription) // Filter relevant events
            .GroupBy(e => e.evt)
            .SelectMany(group =>
            {
                if (group.Key == Subscription.Event.Create)
                {
                    // Apply throttling only to CREATE events
                    return group.Throttle(throttleTime)
                                .Buffer(TimeSpan.FromSeconds(1), 3) // Further control
                                .SelectMany(batch => batch); // Flatten the batch
                }
                else
                {
                    //do something with group !
                    // Pass through other events without throttling
                    return group;
                }
            })
            .Subscribe(e =>
            {
                //ProcessEvent(e, Messages);
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
            return ;

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
            return ;
        }
    }
    static async Task CloudCodeToSetSession()
    {
        // Gather device information
        // Using DeviceInfo.Name as the primary identifier for the session as per your cloud code's query
        var deviceName = DeviceInfo.Name; // This is what your cloud code queries on primarily

        // While deviceId might not be the primary query key in your cloud code,
        // it's still good practice to generate and send it for uniqueness if a deviceName isn't unique enough
        // or if you might change the cloud code later.
        var deviceId = DeviceInfo.Current.Idiom == DeviceIdiom.Desktop ?
                       (DeviceInfo.Current.Name + "_" + DeviceInfo.Current.Model + "_" + System.Net.Dns.GetHostName()).Replace(" ", "_") : // Make it more filesystem/DB friendly
                       DeviceInfo.Current.Platform.ToString() ;

        var deviceIdiom = DeviceInfo.Current.Idiom.ToString();
        var deviceOSVersion = DeviceInfo.Current.VersionString;

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

        var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<object>("updateUserDeviceSession", parameters);

        Debug.WriteLine(result);
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


    public async Task ShareSongOnline(SongModelView song)
    {
        if (song == null || string.IsNullOrEmpty(song.FilePath))
        {
            Debug.WriteLine("Error: Song data or file path is missing.");
            await Shell.Current.DisplayAlert("Error", "Song data or file path is missing.", "OK");
            return;
        }

        if (!File.Exists(song.FilePath))
        {
            Debug.WriteLine($"Error: File not found at path: {song.FilePath}");
            await Shell.Current.DisplayAlert("Error", $"The song file could not be found at:\n{song.FilePath}\nPlease select it again.", "OK");
            return;
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
        }
        catch (ParseFailureException pe)
        {
            // This is where your current error is caught
            Debug.WriteLine($"[SHARE_SONG_ERROR] Parse Exception during file upload: {pe.Code} - {pe.Message}");
            await Shell.Current.DisplayAlert("Upload Error", $"Could not upload song: {pe.Message} (Code: {pe.Code})", "OK");
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SHARE_SONG_ERROR] Generic Exception during file upload or stream open: {ex.Message}");
            Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            await Shell.Current.DisplayAlert("Upload Error", $"An unexpected error occurred while preparing the song: {ex.Message}", "OK");
            return;
        }

        // ... rest of your ParseSong object creation and saving logic
        DimmerSharedSong newSong = new DimmerSharedSong();
        newSong.Title = song.Title;
        newSong.Artist = song.ArtistName;
        newSong.Album = song.AlbumName;
        newSong.DurationSeconds = song.DurationInSeconds;
        newSong.AudioFile = audioFile;
        newSong.Uploader = await ParseClient.Instance.GetCurrentUser();

        try
        {
            await newSong.SaveAsync();
            Debug.WriteLine($"[SHARE_SONG_INFO] ParseSong object saved with ID: {newSong.ObjectId}");
            await Share.RequestAsync($"{newSong.Uploader.Username} Shared {song.Title} with you from Dimmer! :" + audioFile.Url);
        }
        catch (ParseFailureException pe)
        {
            Debug.WriteLine($"[SHARE_SONG_ERROR] Parse Exception during ParseSong save: {pe.Code} - {pe.Message}");
            await Shell.Current.DisplayAlert("Save Error", $"Could not save song details: {pe.Message} (Code: {pe.Code})", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SHARE_SONG_ERROR] Generic Exception during ParseSong save: {ex.Message}");
            await Shell.Current.DisplayAlert("Save Error", $"An unexpected error occurred while saving song details: {ex.Message}", "OK");
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



    void ProcessEvent((Subscription.Event evt, object objectDictionnary, Subscription subscription) e,
                  UserDeviceSession user)
    {

        var objData = e.objectDictionnary as Dictionary<string, object>;
        UserDeviceSession chat;

        switch (e.evt)
        {
            case Subscription.Event.Enter:
                Debug.WriteLine("Entered");
                break;

            case Subscription.Event.Leave:
                Debug.WriteLine("Left");
                break;

            case Subscription.Event.Create:



                break;

            case Subscription.Event.Update:

                break;

            case Subscription.Event.Delete:

                break;

            default:
                Debug.WriteLine("Unhandled event type.");
                break;
        }

        Debug.WriteLine($"Processed {e.evt} on object {objData?.GetType()}");
    }



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

    public async Task SignUpUser(UserModelView user)
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

    public async Task<bool> LoginUser(UserModel usr)
    {
        UserLocalDB = usr;
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
                System.Diagnostics.Debug.WriteLine("Failed to encrypt password for storage.");
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
            e.EventType == Subscription.Event.Create )
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

    // Remember to unsubscribe when done:
    // if (_presenceSubscription != null) await liveQueryClient.Unsubscribe(_presenceSubscription);


    // In Dimmer.DimmerLive.DimmerLiveStateService.cs

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


    
}
