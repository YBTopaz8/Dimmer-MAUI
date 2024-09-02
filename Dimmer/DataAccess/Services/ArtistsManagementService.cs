
namespace Dimmer_MAUI.DataAccess.Services;

public class ArtistsManagementService : IArtistsManagementService
{
    Realm db;
    public IDataBaseService DataBaseService { get; }
    public ISongsManagementService SongsManagementService { get; }
    public IList<ArtistModelView> AllArtists { get; set; }
    public IList<AlbumArtistSongLink> AlbumsArtistsSongLink { get; set; }
    public ObservableCollection<ArtistModelView> SongsFromSpecificArtist { get; set; }

    public ArtistsManagementService(IDataBaseService dataBaseService, ISongsManagementService songsManagementService)
    {
        DataBaseService = dataBaseService;
        SongsManagementService = songsManagementService;
        OpenDB();
        GetArtists();
    }
    Realm OpenDB()
    {
        db = DataBaseService.GetRealm();
        return db;
    }
    
    public bool DeleteArtist(ObjectId artistID)
    {
        throw new NotImplementedException();
    }

    public void GetArtists()
    {
        try
        {
            var realmArtists = db.All<ArtistModel>().ToList();
            AllArtists = new List<ArtistModelView>(realmArtists.Select(artist => new ArtistModelView(artist)));
            AllArtists ??= Enumerable.Empty<ArtistModelView>().ToList();

            var realmArtistsAlbumSongLink = db.All<AlbumArtistSongLink>().ToList();
            
            //AllArtists = new List<ArtistModelView>(realmArtistsAlbumSongLink.Select(artist => new ArtistModelView(artist)));
            AlbumsArtistsSongLink ??= Enumerable.Empty<AlbumArtistSongLink>().ToList();

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting Artists: {ex.Message}");
        }
    }

    public IList<ObjectId> GetSongsIDsFromArtistID(ObjectId artistID)
    {
        try
        {
            var specificArtist = AllArtists.FirstOrDefault(x => x.Id == artistID);
            if (specificArtist is not null)
            {
                var songIds = db.All<AlbumArtistSongLink>()
                    .Where(link => link.ArtistId == artistID)
                    .ToList()
                    .Select(link => link.SongId)
                    .ToList();
                return songIds is not null ? songIds : Enumerable.Empty<ObjectId>().ToList();
            }
            return Enumerable.Empty<ObjectId>().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return Enumerable.Empty<ObjectId>().ToList();
        }
    }

    public bool UpdateArtist(ArtistModelView artistModel)
    {
        try
        {
            var artist = db.Find<ArtistModel>(artistModel.Id);
            if (artist is not null)
            {
                var realmArtist = new ArtistModel(artistModel);
                artist = realmArtist;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            
            Debug.WriteLine($"Error renaming artist: {ex.Message}");
            return false; 
        }
    }

    private static List<ArtistModel> globalArtists = new List<ArtistModel>();

    public bool AddSongToArtistWithArtistIDAndAlbum(List<ArtistModelView> artistModels, List<AlbumModelView> albumModels, List<AlbumArtistSongLink> links, List<SongsModel> songs)
    {
        try
        {
            db.Write(() =>
            {
                foreach (var song in songs)
                {
                    db.Add(song);
                    Debug.WriteLine("Added Song " + song.Title);
                }
            });
            // Insert new artists
            db.Write(() =>
            {
                foreach (var artistModel in artistModels)
                {
                    var existingArtist = db.Find<ArtistModel>(artistModel.Id);
                    if (existingArtist == null)
                    {
                        db.Add(new ArtistModel
                        {
                            Id = artistModel.Id,
                            Name = artistModel.Name
                        });
                        Debug.WriteLine("Added Artist");
                    }
                    else
                    {
                        Debug.WriteLine($"Artist {artistModel.Id} already exists.");
                    }
                }
            });

            //Insert new albums
            db.Write(() =>
            {
                foreach (var albumModel in albumModels)
                {
                    var existingAlbum = db.Find<AlbumModel>(albumModel.Id);
                    if (existingAlbum == null)
                    {
                        db.Add(new AlbumModel
                        {
                            Id = albumModel.Id,
                            Name = albumModel.Name,
                            ImagePath = albumModel.AlbumImagePath,
                        });
                        Debug.WriteLine("Added Album");
                    }
                    else
                    {
                        existingAlbum.Name = albumModel.Name;
                        Debug.WriteLine($"Album {albumModel.Id} updated.");
                    }
                    
                }
            });

            // Insert new album-artist-song links
            db.Write(() =>
            {
                foreach (var link in links)
                {
                    var allLinks = db.All<AlbumArtistSongLink>().ToList();
                    AlbumArtistSongLink existingLink = new();
                    if (allLinks is not null || allLinks?.Count > 0)
                    {
                        existingLink = allLinks.FirstOrDefault(l => l.ArtistId == link.ArtistId && l.SongId == link.SongId && l.AlbumId == link.AlbumId);
                    }
                    if (existingLink == null)
                    {
                        db.Add(new AlbumArtistSongLink
                        {
                            ArtistId = link.ArtistId,
                            SongId = link.SongId,
                            AlbumId = link.AlbumId
                        });
                        Debug.WriteLine("Added Link");
                    }
                    else
                    {
                        Debug.WriteLine($"Link {link.ArtistId}-{link.SongId}-{link.AlbumId} already exists.");
                    }
                }
            });

            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Debug.WriteLine($"Out of range error when adding to artist, song, and album: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception when adding to artist, song, and album: {ex.Message}");
            return false;
        }
    }




    public bool AddSongToArtistWithArtistID(SongsModelView song, ArtistModelView Artist)
    {
        throw new NotImplementedException();
    }
}
