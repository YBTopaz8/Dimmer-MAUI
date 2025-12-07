namespace Dimmer.Interfaces.Services;

/// <summary>
/// A service dedicated to performing complex data operations on the Realm database,
/// ensuring data integrity and handling all object relationships correctly.
/// </summary>
public class MusicDataService
{
    private readonly IRealmFactory _realmFactory; 
    private readonly ILogger<MusicDataService> _logger;

    public MusicDataService(IRealmFactory realmFactory, ILogger<MusicDataService> logger)
    {
        _realmFactory = realmFactory;
        _logger = logger;
    }

    #region Song Relationship Management

    /// <summary>
    /// Updates the artist(s) for a specific song. It finds or creates the new artists,
    /// updates the relationships, and cleans up any orphaned artists.
    /// </summary>
    /// <param name="songId">The ObjectId of the song to update.</param>
    /// <param name="newArtistNames">An enumerable of strings for the new artist names.</param>
    public async Task UpdateSongArtists(ObjectId songId, IEnumerable<string> newArtistNames)
    {
        if (songId == ObjectId.Empty || newArtistNames == null || !newArtistNames.Any())
        {
            _logger.LogWarning("UpdateSongArtists called with invalid parameters.");
            return;
        }

        using var realm = _realmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(songId);
            if (songInDb == null)
            {
                _logger.LogWarning("Could not find song with ID {SongId} to update artists.", songId);
                return;
            }

            // 1. Store old artists for potential cleanup later
            var oldArtists = songInDb.ArtistToSong.ToList();

            // 2. Clear existing artist relationships for the song
            songInDb.ArtistToSong.Clear();
            var newArtistsList = new List<ArtistModel>();

            // 3. Find or create new artists and add them to the song
            foreach (var artistName in newArtistNames.Select(n => n.Trim()).Distinct())
            {
                if (string.IsNullOrWhiteSpace(artistName))
                    continue;

                var existingArtist = realm.All<ArtistModel>().FirstOrDefault(a => a.Name == artistName);
                ArtistModel artistToAssign;

                if (existingArtist != null)
                {
                    artistToAssign = existingArtist;
                }
                else
                {
                    _logger.LogInformation("Creating new artist '{ArtistName}'.", artistName);
                    artistToAssign = new ArtistModel { Name = artistName };
                    realm.Add(artistToAssign);
                }
                newArtistsList.Add(artistToAssign);
            }

            // Add all new artists to the song's relationship list
            foreach (var artist in newArtistsList)
            {
                songInDb.ArtistToSong.Add(artist);
            }

            // 4. Update the primary Artist and ArtistName fields for display/simplicity
            songInDb.Artist = newArtistsList.First();
            songInDb.OtherArtistsName = string.Join(", ", newArtistsList.Select(a => a.Name));

            // 5. (Crucial) Clean up orphaned artists.
            foreach (var oldArtist in oldArtists)
            {
                // If this artist is not in the new list AND no other songs or albums are linked to it...
                if (!newArtistsList.Contains(oldArtist) && !oldArtist.Songs.Any() && !oldArtist.Albums.Any())
                {
                    _logger.LogInformation("Removing orphaned artist '{ArtistName}' with ID {ArtistId}.", oldArtist.Name, oldArtist.Id);
                    realm.Remove(oldArtist);
                }
            }
        });
        _logger.LogInformation("Successfully updated artists for song ID {SongId}.", songId);
    }

    /// <summary>
    /// Updates the album for a specific song. It finds or creates the album/artist,
    /// updates relationships, and cleans up any orphaned albums.
    /// </summary>
    /// <param name="songId">The ObjectId of the song to update.</param>
    /// <param name="newAlbumName">The name of the new album.</param>
    /// <param name="artistNameForAlbum">The name of the album's primary artist. This is crucial for uniqueness.</param>
    public async Task UpdateSongAlbum(ObjectId songId, string newAlbumName, string artistNameForAlbum)
    {
        if (songId == ObjectId.Empty || string.IsNullOrWhiteSpace(newAlbumName) || string.IsNullOrWhiteSpace(artistNameForAlbum))
            return;

        using var realm = _realmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(songId);
            if (songInDb == null)
                return;

            var oldAlbum = songInDb.Album;

            // Find or create the artist for the album first
            var albumArtist = realm.All<ArtistModel>().FirstOrDefault(a => a.Name == artistNameForAlbum);
            if (albumArtist == null)
            {
                albumArtist = new ArtistModel { Name = artistNameForAlbum };
                realm.Add(albumArtist);
            }

            // Now, find the album by its name AND artist. This is the correct way to identify an album.
            var existingAlbum = realm.All<AlbumModel>().FirstOrDefault(a => a.Name == newAlbumName && a.Artist == albumArtist);
            AlbumModel albumToAssign;

            if (existingAlbum != null)
            {
                albumToAssign = existingAlbum;
            }
            else
            {
                albumToAssign = new AlbumModel { Name = newAlbumName, Artist = albumArtist };
                // Also link the artist to the album
                albumToAssign.Artists.Add(albumArtist);
                realm.Add(albumToAssign);
            }

            songInDb.Album = albumToAssign;
            songInDb.AlbumName = albumToAssign.Name;

            // Cleanup orphaned album
            if (oldAlbum != null && oldAlbum != albumToAssign && !oldAlbum.SongsInAlbum.Any())
            {
                _logger.LogInformation("Removing orphaned album '{AlbumName}' by {ArtistName}.", oldAlbum.Name, oldAlbum.Artist?.Name);
                realm.Remove(oldAlbum);
            }
        });
    }

    /// <summary>
    /// Updates the genre for a specific song.
    /// </summary>
    public async Task UpdateSongGenre(ObjectId songId, string newGenreName)
    {
        if (songId == ObjectId.Empty || string.IsNullOrWhiteSpace(newGenreName))
            return;

        using var realm = _realmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(songId);
            if (songInDb == null)
                return;

            // Note: We don't clean up old genres here, as they are often broad categories
            // that should persist even if no songs currently use them.

            var existingGenre = realm.All<GenreModel>().FirstOrDefault(g => g.Name == newGenreName);
            GenreModel genreToAssign;

            if (existingGenre != null)
            {
                genreToAssign = existingGenre;
            }
            else
            {
                genreToAssign = new GenreModel { Name = newGenreName };
                realm.Add(genreToAssign);
            }

            songInDb.Genre = genreToAssign;
            songInDb.GenreName = genreToAssign.Name; // Keep denormalized data in sync
        });
    }

    #endregion

    #region User Note Management

    /// <summary>
    /// Adds a new user note to a song. This replaces the legacy method.
    /// </summary>
    /// <param name="songId">The ObjectId of the song to add the note to.</param>
    /// <param name="noteText">The text content of the note.</param>
    /// <returns>The created UserNoteModel, or null if the song was not found.</returns>
    public async Task<UserNoteModel?> AddNoteToSong(ObjectId songId, string noteText)
    {
        if (songId == ObjectId.Empty || string.IsNullOrWhiteSpace(noteText))
            return null;


        using var realm = _realmFactory.GetRealmInstance();

        var existingSong = realm.Find<SongModel>(songId);
        await realm.WriteAsync(() =>
        {
            if (existingSong != null)
            {
                existingSong.UserNotes.Add(new UserNoteModel()
                {
                    UserMessageText= noteText
                });
                _logger.LogInformation("Added note to song '{SongTitle}'.", existingSong.Title);
            }
            else
            {
                _logger.LogWarning("Could not find song with ID {SongId} to add note.", songId);
            }
        });

        // The note object is now managed by Realm, but we can return it.
        return existingSong?.UserNotes.LastOrDefault();
    }

    // You can also add methods to update or remove notes
    public async Task RemoveNoteFromSong(ObjectId songId, string noteId)
    {
        using var realm = _realmFactory.GetRealmInstance();
        await realm.WriteAsync(() => {
            var song = realm.Find<SongModel>(songId);
            var noteToRemove = song?.UserNotes.FirstOrDefault(n => n.Id == noteId);
            if (noteToRemove != null)
            {
                song.UserNotes.Remove(noteToRemove);
            }
        });
    }


    #endregion

    #region Playlist Management

    /// <summary>
    /// Adds a collection of songs to a manual playlist.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist to modify.</param>
    /// <param name="songIdsToAdd">A list of song IDs to add.</param>
    public async Task AddSongsToPlaylist(ObjectId playlistId, IEnumerable<ObjectId> songIdsToAdd)
    {
        if (playlistId == ObjectId.Empty || !songIdsToAdd.Any())
            return;

        using var realm = _realmFactory.GetRealmInstance();
        await realm.WriteAsync(() =>
        {
            var playlist = realm.Find<PlaylistModel>(playlistId);
            if (playlist == null || playlist.IsSmartPlaylist)
            {
                _logger.LogWarning("Playlist {PlaylistId} not found or is a smart playlist.", playlistId);
                return;
            }

            foreach (var songId in songIdsToAdd)
            {
                // Avoid adding duplicates
                if (!playlist.ManualSongIds.Contains(songId))
                {
                    playlist.ManualSongIds.Add(songId);
                }
            }
        });
    }

    /// <summary>
    /// Removes a collection of songs from a manual playlist.
    /// </summary>
    public async Task RemoveSongsFromPlaylist(ObjectId playlistId, IEnumerable<ObjectId> songIdsToRemove)
    {
        if (playlistId == ObjectId.Empty || !songIdsToRemove.Any())
            return;

        using var realm = _realmFactory.GetRealmInstance();
        await realm.WriteAsync(() =>
        {
            var playlist = realm.Find<PlaylistModel>(playlistId);
            if (playlist == null || playlist.IsSmartPlaylist)
                return;

            foreach (var songId in songIdsToRemove)
            {
                playlist.ManualSongIds.Remove(songId);
            }
        });
    }
    #endregion
    // Add this method to your MusicDataService.cs

    /// <summary>
    /// A comprehensive method to update all editable song metadata from a single source object.
    /// This is designed to be called from an "Edit Song" page's save button.
    /// It handles simple properties and calls specialized methods for complex relationships,
    /// all within a single atomic transaction.
    /// </summary>
    /// <param name="songView">A SongModelView object containing all the new data from the edit page.</param>
    public async Task UpdateFullSongDetails(SongModelView songView)
    {
        if (songView == null)
        {
            _logger.LogWarning("UpdateFullSongDetails called with a null song view.");
            return;
        }

        using var transactionRealm = _realmFactory.GetRealmInstance();

        // Use a single WriteAsync block for atomicity. All changes succeed or none do.
        await transactionRealm.WriteAsync(() =>
        {
            var songInDb = transactionRealm.Find<SongModel>(songView.Id);
            if (songInDb == null)
            {
                _logger.LogWarning("Could not find song with ID {SongId} to update.", songView.Id);
                return;
            }

            _logger.LogInformation("Starting full metadata update for song '{Title}' (ID: {Id})", songInDb.Title, songInDb.Id);

            // === 1. Update Simple Properties ===
            // These are direct assignments from the view model to the database model.
            songInDb.SetTitleAndDuration(songView.Title, songView.DurationInSeconds); // Use the setter to update the key
            songInDb.ReleaseYear = songView.ReleaseYear;
            songInDb.TrackNumber = songView.TrackNumber;
            songInDb.DiscNumber = songView.DiscNumber;
            songInDb.Rating = songView.Rating;
            songInDb.IsFavorite = songView.IsFavorite;
            songInDb.Composer = songView.Composer;
            songInDb.Lyricist = songView.Lyricist;
            songInDb.UnSyncLyrics = songView.UnSyncLyrics;
            songInDb.SyncLyrics = songView.SyncLyrics;
            songInDb.HasLyrics = !string.IsNullOrWhiteSpace(songView.UnSyncLyrics) || !string.IsNullOrWhiteSpace(songView.SyncLyrics);
            songInDb.HasSyncedLyrics = !string.IsNullOrWhiteSpace(songView.SyncLyrics);
            songInDb.CoverImagePath = songView.CoverImagePath; // Update cover path
            songInDb.LastDateUpdated = DateTimeOffset.UtcNow;
            songInDb.Achievement = songView.Achievement;
            songInDb.Conductor = songView.Conductor;
            songInDb.BitDepth = songView.BitDepth;
            songInDb.BPM = songView.BPM;
            songInDb.Description = songView.Description;
            

            // === 2. Handle Complex Relationships using our existing logic ===
            // We re-implement the logic from the other service methods here to ensure
            // they operate within the same transaction.

            // --- Update Artists ---
            var newArtistNames = (songView.ArtistName ?? string.Empty)
                .Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .ToList();

            if (newArtistNames.Count!=0)
            {
                var oldArtists = songInDb.ArtistToSong.ToList();
                songInDb.ArtistToSong.Clear();
                var newArtistsList = new List<ArtistModel>();

                foreach (var artistName in newArtistNames)
                {
                    var artist = transactionRealm.All<ArtistModel>().FirstOrDefault(a => a.Name == artistName)
                                  ?? transactionRealm.Add(new ArtistModel { Name = artistName });
                    newArtistsList.Add(artist);
                    songInDb.ArtistToSong.Add(artist);
                }
                var firstArtist = newArtistsList.FirstOrDefault();
                if (firstArtist is not null)
                    songInDb.Artist = firstArtist;

                songInDb.ArtistName = string.Join(" | ", newArtistsList.Select(a => a.Name));

                // Cleanup
                foreach (var oldArtist in oldArtists.Where(oa => !newArtistsList.Contains(oa)))
                {
                    if (!oldArtist.Songs.Any() && !oldArtist.Albums.Any())
                    {
                        transactionRealm.Remove(oldArtist);
                    }
                }
            }


            // --- Update Album ---
            if (!string.IsNullOrWhiteSpace(songView.AlbumName))
            {
                // The primary artist of the album is typically the first artist of the song.
                var albumArtistName = newArtistNames.FirstOrDefault();
                if (albumArtistName != null)
                {
                    var oldAlbum = songInDb.Album;

                    var albumArtist = transactionRealm.All<ArtistModel>().FirstOrDefault(a => a.Name == albumArtistName)
                                      ?? songInDb.Artist; // Fallback to existing artist if new one not found

                    if (albumArtist != null)
                    {
                        var album = transactionRealm.All<AlbumModel>().FirstOrDefault(a => a.Name == songView.AlbumName && a.Artist == albumArtist)
                                   ?? transactionRealm.Add(new AlbumModel { Name = songView.AlbumName, Artist = albumArtist });

                        if (!album.Artists.Contains(albumArtist))
                        {
                            album.Artists.Add(albumArtist);
                        }

                        songInDb.Album = album;
                        songInDb.AlbumName = album.Name;

                        // Cleanup
                        if (oldAlbum != null && oldAlbum != album && !oldAlbum.SongsInAlbum.Any())
                        {
                            transactionRealm.Remove(oldAlbum);
                        }
                    }
                }
            }

            // --- Update Genre ---
            if (!string.IsNullOrWhiteSpace(songView.GenreName))
            {
                var genre = transactionRealm.All<GenreModel>().FirstOrDefault(g => g.Name == songView.GenreName)
                            ?? transactionRealm.Add(new GenreModel { Name = songView.GenreName });
                songInDb.Genre = genre;
                songInDb.GenreName = genre.Name;
            }

        }); // The atomic transaction ends here.

        _logger.LogInformation("Successfully saved all changes for song ID {SongId}", songView.Id);
    }
}