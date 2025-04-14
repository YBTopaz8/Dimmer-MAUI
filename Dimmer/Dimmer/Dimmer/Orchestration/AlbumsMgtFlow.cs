using Dimmer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Orchestration;
public partial class AlbumsMgtFlow : BaseAppFlow
{
    private readonly IRealmFactory _realmFactory;
    private readonly IMapper Mapper;
    public IDimmerAudioService AudioService { get; }
    public static BehaviorSubject<List<AlbumModel>> SpecificAlbums { get; } = new([]);
    public AlbumsMgtFlow(IRealmFactory realmFactory, IDimmerAudioService dimmerAudioService, IMapper mapper) : base(realmFactory, dimmerAudioService, mapper)
    {
        _realmFactory = realmFactory;
        AudioService=dimmerAudioService;
        Mapper=mapper;
        _realmFactory.GetRealmInstance();
        SubscribeToSpecificAlbumsChange();
    }

    private void SubscribeToSpecificAlbumsChange()
    {
        
    }
    public void AddAlbum(AlbumModel album)
    {
        using var realm = _realmFactory.GetRealmInstance();
        realm.Write(() =>
        {
            realm.Add(album);
        });
    }
    public void RemoveAlbum(AlbumModel album)
    {
        using var realm = _realmFactory.GetRealmInstance();
        realm.Write(() =>
        {
            realm.Remove(album);
        });
    }
    public void UpdateAlbum(AlbumModel album)
    {
        using var realm = _realmFactory.GetRealmInstance();
        realm.Write(() =>
        {
            realm.Add(album, update: true);
        });
    }
    public void ClearAlbums()
    {
        using var realm = _realmFactory.GetRealmInstance();
        realm.Write(() =>
        {
            realm.RemoveAll<AlbumModel>();
        });
    }
    public void GetAllAlbums()
    {
        using var realm = _realmFactory.GetRealmInstance();
        var albums = realm.All<AlbumModel>().ToList();
        SpecificAlbums.OnNext(albums);
    }
    public void GetAlbumById(string id)
    {
        using var realm = _realmFactory.GetRealmInstance();
        var album = realm.Find<AlbumModel>(id);
        if (album != null)
        {
            SpecificAlbums.OnNext(new List<AlbumModel> { album });
        }
        else
        {
            SpecificAlbums.OnNext(new List<AlbumModel>());
        }
    }
    public void GetAlbumsByName(string Name)
    {
        using var realm = _realmFactory.GetRealmInstance();
        var albums = realm.All<AlbumModel>().Where(a => a.Name == Name).ToList();
        SpecificAlbums.OnNext(albums);
    }
    public void GetAlbumsByTrackCount(int trackCount)
    {
        using var realm = _realmFactory.GetRealmInstance();
        var albums = realm.All<AlbumModel>().Where(a => a.NumberOfTracks == trackCount).ToList();
        SpecificAlbums.OnNext(albums);
    }
    public void GetAlbumsByDuration(string duration)
    {
        using var realm = _realmFactory.GetRealmInstance();
        var albums = realm.All<AlbumModel>().Where(a => a.TotalDuration == duration).ToList();
        SpecificAlbums.OnNext(albums);
    }
    public void GetAlbumsBySongModel(SongModelView songModelView)
    {
        SongModel song = Mapper.Map<SongModel>(songModelView);
        using var realm = _realmFactory.GetRealmInstance();

        // Step 1: Get all AlbumIds from links for the given SongId
        var albumIds = realm.All<AlbumArtistGenreSongLink>()
                            .Where(a => a.SongId == song.LocalDeviceId)
                            .AsEnumerable() // materialize first
                            .Select(a => a.AlbumId)
                            .Distinct() // now allowed in LINQ-to-objects
                            .ToList();

        // Step 2: Fetch matching albums using manual filtering
        var allAlbums = realm.All<AlbumModel>().ToList(); // force materialize
        var matchedAlbums = allAlbums
            .Where(album => albumIds.Contains(album.LocalDeviceId))
            .ToList();

        // Step 3: Emit result
        SpecificAlbums.OnNext(matchedAlbums);
    }

}
