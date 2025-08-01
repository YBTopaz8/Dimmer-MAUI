using DynamicData;

namespace Dimmer.DimmerLive.Interfaces;

// The interface for our new service
public interface ILiveRealmService : IDisposable
{
    // Exposes the LIVE, observable change stream for songs.
    // This is what DynamicData will connect to.
    IObservable<IChangeSet<SongModel, ObjectId>> Songs { get; }

    // We can add others later if needed
    IObservable<IChangeSet<ArtistModel, ObjectId>> Artists { get; }
}

// The implementation of the service
public class LiveRealmService : ILiveRealmService
{
    private readonly Realm _realm; // This instance stays open for the lifetime of the service
    private readonly IDisposable _realmSubscription;

    public IObservable<IChangeSet<SongModel, ObjectId>> Songs { get; }
    public IObservable<IChangeSet<ArtistModel, ObjectId>> Artists { get; }


    public LiveRealmService(IRealmFactory factory)
    {
        _realm = factory.GetRealmInstance();
        var songCount = _realm.All<SongModel>().Count();

        System.Diagnostics.Debug.WriteLine($"[LiveRealmService] Initialized. Found {songCount} songs in DB.");
        // 1. Get the live, notifying IRealmCollection.
        var liveSongs = _realm.All<SongModel>().AsRealmCollection();
        var liveArtists = _realm.All<ArtistModel>().AsRealmCollection();

        // 2. Use the ONE extension method that does everything.
        // It converts the live collection directly into the IChangeSet stream
        // that DynamicData needs. No intermediate observables.
        Songs = liveSongs.AsObservableChangeSet<SongModel, ObjectId>(s => s.Id);
        Artists = liveArtists.AsObservableChangeSet<ArtistModel, ObjectId>(a => a.Id);
        // Use your actual _logger here
    }


    public void Dispose()
    {
        // This will be called when the app closes, ensuring the Realm instance is cleaned up.
        _realm?.Dispose();
    }
}