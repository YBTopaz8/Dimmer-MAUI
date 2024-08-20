
using Dimmer.Models;
using System.Collections.ObjectModel;

namespace Dimmer.DataAccess.Services;
public class PlayListManagementService : IPlaylistManagementService
{
    Realm db;
    public IDataBaseService DataBaseService { get; }
    public IList<PlaylistModelView> AllPlaylists { get; set; }
    public ObservableCollection<SongsModelView> SongsFromSpecificPlaylist { get; set; }
    public PlayListManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        OpenDB();
        GetPlaylists();
    }

    Realm OpenDB()
    {
        db = DataBaseService.GetRealm();
        return db;
    }
    
    public void GetPlaylists()
    {
        try
        {
            AllPlaylists?.Clear();            
            var realmPlayLists = db.All<PlaylistModel>().ToList();
            AllPlaylists = new List<PlaylistModelView>(realmPlayLists.Select(playlist => new PlaylistModelView(playlist)));            
            AllPlaylists ??= Enumerable.Empty<PlaylistModelView>().ToList();
            Debug.WriteLine($"Playlist Count {AllPlaylists.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting playlists: {ex.Message}");
        }
    }

    public IList<ObjectId> GetSongsIDsFromPlaylistID(ObjectId playlistID)
    {
        try
        {
            // Get the playlist
            var specificPlaylist = AllPlaylists
                .FirstOrDefault(x => x.Id == playlistID);

            if (specificPlaylist != null)
            {                
                // Get the song IDs associated with this playlist
                var songIds = db.All<PlaylistSongLink>()
                                .Where(link => link.PlaylistId == playlistID)
                                .ToList()
                                .Select(link => link.SongId)
                                .ToList();

                return songIds is not null? songIds : Enumerable.Empty<ObjectId>().ToList();
            }
            
            return Enumerable.Empty<ObjectId>().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return Enumerable.Empty<ObjectId>().ToList();
        }
    }

    public bool AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {
        try
        {
            db.Write(() =>
            {
                var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Id == playlistID);
                if (specificPlaylist == null)
                {
                    specificPlaylist = new PlaylistModel
                    {
                        Name = $"New Playlist {AllPlaylists.Count + 1}",
                        DateCreated = DateTimeOffset.Now,
                    };
                    db.Add(specificPlaylist);
                }
                var existingLink = db.All<PlaylistSongLink>()
                    .FirstOrDefault(link => link.PlaylistId == playlistID && link.SongId == song.Id);

                if (existingLink == null)
                {
                    // Add the new link
                    db.Add(new PlaylistSongLink
                    {
                        PlaylistId = playlistID,
                        SongId = song.Id
                    });

                    // Update playlist properties
                    specificPlaylist.TotalDuration += song.DurationInSeconds;
                    specificPlaylist.TotalSize += song.FileSize;
                    specificPlaylist.TotalSongsCount += 1;
                }
            });

            GetPlaylists();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exception when adding to playlist: {ex.Message}");
            return false;
        }
    }

    public bool AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName)
    {
        try
        {
            ObjectId plID = ObjectId.Empty;

            db.Write(() =>
            {
                // Retrieve the playlist using a safer approach
                PlaylistModel? specificPlaylist = null;
                foreach (var playlist in db.All<PlaylistModel>())
                {
                    if (playlist.Name == playlistName)
                    {
                        specificPlaylist = playlist;
                        break;
                    }
                }

                // If no playlist is found, create a new one
                if (specificPlaylist == null)
                {
                    specificPlaylist = new PlaylistModel
                    {
                        Name = playlistName,
                        DateCreated = DateTimeOffset.Now,
                    };
                    db.Add(specificPlaylist);
                }

                // Retrieve the existing link, if any
                PlaylistSongLink? existingLink = null;
                foreach (var link in db.All<PlaylistSongLink>())
                {
                    if (link.PlaylistId == specificPlaylist.Id && link.SongId == song.Id)
                    {
                        existingLink = link;
                        break;
                    }
                }

                // If no link exists, create it and update the playlist
                if (existingLink == null)
                {
                    db.Add(new PlaylistSongLink
                    {
                        PlaylistId = specificPlaylist.Id,
                        SongId = song.Id
                    });

                    specificPlaylist.TotalDuration += song.DurationInSeconds;
                    specificPlaylist.TotalSize += song.FileSize;
                    specificPlaylist.TotalSongsCount += 1;
                }

                plID = specificPlaylist.Id;
            });

            GetPlaylists();
            GetSongsIDsFromPlaylistID(plID);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exception when adding to playlist: {ex.Message}");
            return false;
        }
    }

    public bool RemoveSongFromPlayListWithPlayListName(SongsModelView song, string playlistName)
    {
        try
        {
            db.Write(() =>
            {
                var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Name == playlistName);
                if (specificPlaylist is null)
                {
                    Debug.WriteLine("Playlist not found");
                    //Shell.Current.DisplayAlert("Error", "Playlist not found", "OK");
                    //return false;
                }
                var existingLink = db.All<PlaylistSongLink>()
                    .FirstOrDefault(link => link.PlaylistId == specificPlaylist.Id && link.SongId == song.Id);

                if (existingLink is not null)
                {
                    db.Remove(existingLink);
                }

                specificPlaylist.TotalDuration -= song.DurationInSeconds;
                specificPlaylist.TotalSize -= song.FileSize;
                specificPlaylist.TotalSongsCount -= 1;
            });
            //GetPlayLists();
            return true;
        }
        catch (Exception ex)
        {

            Debug.WriteLine("Error when removingSongfrom playing with name " + ex.Message);
            throw new Exception("Error when removing from playlist" + ex.Message);
        }
    }

    public bool RemoveSongFromPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {
        try
        {
            db.Write(() =>
            {
                var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Id == playlistID);
                if (specificPlaylist == null)
                {
                    Debug.WriteLine("Playlist not found.");
                    //return false;
                }

                // Find the link for the song and playlist
                var linkToRemove = db.All<PlaylistSongLink>()
                    .FirstOrDefault(link => link.PlaylistId == playlistID && link.SongId == song.Id);

                if (linkToRemove != null)
                {
                    // Remove the link
                    db.Remove(linkToRemove);

                    // Update playlist properties
                    specificPlaylist.TotalDuration -= song.DurationInSeconds;
                    specificPlaylist.TotalSize -= song.FileSize;
                    specificPlaylist.TotalSongsCount -= 1;
                }
            });

            GetPlaylists();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exception when removing from playlist: {ex.Message}");
            return false;
        }
    }


    public bool DeletePlaylist(ObjectId playlistID)
    {
        try
        {
            var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Id == playlistID);
            db.Write(() =>
            {
                db.Remove(specificPlaylist);
            });

            GetPlaylists();
            return true;
        }
        catch (Exception ex)
        {

            Debug.WriteLine("Error when deleting playlist " + ex.Message);
            throw new Exception("Error when deleting playlist" + ex.Message);
            
        }
    }

    public bool RenamePlaylist(ObjectId playlistID, string newPlaylistName)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Id == playlistID);
        if (specificPlaylist is null)
        {
            //await Shell.Current.DisplayAlert("Error While Renaming", "No Such Playlist Exists", "OK");
            return false;
        }
        db.Write(() =>
        {
            specificPlaylist.Name = newPlaylistName;
        });

        GetPlaylists();
        return true;
    }
}
