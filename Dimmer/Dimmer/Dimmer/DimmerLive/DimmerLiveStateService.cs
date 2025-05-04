using Dimmer.DimmerLive.Interfaces;
using Dimmer.DimmerLive.Models;
using Dimmer.Utilities.Extensions;
using Parse.LiveQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive;
public class DimmerLiveStateService : IDimmerLiveStateService
{

    private bool isConnected = false;
    private ParseLiveQueryClient? LiveClient { get; set; }
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
       
    }

    public void DeleteUserLocally(UserModel user)
    {
        _userRepo.Delete(user);
    }

    public async Task DeleteUserOnline(ParseUser user)
    {
        await user.DeleteAsync();
        
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
    }

    public void RequestSongFromDifferentDevice(string userId, string songId, string deviceId)
    {
        // use a parse cloud code, send
    }

    public void SaveUserLocally(UserModelView user)
    {
        var usr = mapper.Map<UserModel>(user);
        _userRepo.AddOrUpdate(usr);
    }

    public void SaveUserOnline(UserModelView user)
    {
        throw new NotImplementedException();
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

    public void GetAllConnectedDeviced(ParseUser currentUser)
    {

        // parse cloud code

        // i will return a list of devices from devicesession etc etc

    }

    async Task SetupLiveQuery()
    {
        try
        {
            var query = ParseClient.Instance.GetQuery<UserDeviceSession>();
            var subscription =  LiveClient!.Subscribe(query);

            LiveClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;

            LiveClient.OnConnected
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
                            isConnected = false;
                            Debug.WriteLine($"Retry attempt {tuple.attempt} after {retryDelaySeconds} seconds...");

                            // Explicit reconnect call before retry delay
                            LiveClient.ConnectIfNeeded(); // revive app!

                            return Observable.Timer(TimeSpan.FromSeconds(retryDelaySeconds)).Select(_ => tuple.error); // Maintain compatible type
                        })
                )
                .Subscribe(
                    _ =>
                    {
                        isConnected=true;
                        Debug.WriteLine("Reconnected successfully.");
                    },
                    ex => Debug.WriteLine($"Failed to reconnect: {ex.Message}")
                );

            LiveClient.OnError
                .Do(async ex =>
                {
                    Debug.WriteLine("LQ Error: " + ex.Message);
                    LiveClient.ConnectIfNeeded();  // Ensure reconnection on errors
                })
                .OnErrorResumeNext(Observable.Empty<Exception>()) // Prevent breaking the stream
                .Subscribe();

            LiveClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();


            LiveClient.OnSubscribed
                .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
                .Subscribe();

            int batchSize = 3; // Number of events to release at a time
            TimeSpan throttleTime = TimeSpan.FromMilliseconds(0000);

            LiveClient.OnObjectEvent
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
                LiveClient.OnConnected.Select(_ => "Connected"),
                LiveClient.OnDisconnected.Select(_ => "Disconnected"),
                (connected, disconnected) => $"Status: {connected}, {disconnected}"
            )
            .Throttle(TimeSpan.FromSeconds(1)) // Aggregate status changes
            .Subscribe(status => Debug.WriteLine(status));
        }
        catch (Exception ex)
        {
            Debug.WriteLine("SetupLiveQuery Error: " + ex.Message);
        }
    }
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

}
