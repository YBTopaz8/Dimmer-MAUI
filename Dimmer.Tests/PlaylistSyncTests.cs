using Dimmer.Data.Models;
using MongoDB.Bson;
using Realms;
using Xunit;

namespace Dimmer.Tests;

/// <summary>
/// Tests for custom playlist functionality including note-based playlist synchronization.
/// </summary>
public class PlaylistSyncTests : IDisposable
{
    private readonly Realm _realm;
    private readonly string _testDbPath;

    public PlaylistSyncTests()
    {
        // Create a unique in-memory database for each test
        _testDbPath = $"test_{Guid.NewGuid()}.realm";
        var config = new RealmConfiguration(_testDbPath)
        {
            IsReadOnly = false,
            SchemaVersion = 1
        };
        _realm = Realm.GetInstance(config);
    }

    [Fact]
    public void PlaylistModel_CanBeCreated()
    {
        // Arrange
        var playlistName = "Test Playlist";
        var description = "A test playlist";

        // Act
        _realm.Write(() =>
        {
            var playlist = new PlaylistModel
            {
                Id = ObjectId.GenerateNewId(),
                PlaylistName = playlistName,
                Description = description,
                IsSmartPlaylist = false,
                DateCreated = DateTimeOffset.UtcNow
            };
            _realm.Add(playlist);
        });

        // Assert
        var savedPlaylist = _realm.All<PlaylistModel>().FirstOrDefault();
        Assert.NotNull(savedPlaylist);
        Assert.Equal(playlistName, savedPlaylist.PlaylistName);
        Assert.Equal(description, savedPlaylist.Description);
        Assert.False(savedPlaylist.IsSmartPlaylist);
    }

    [Fact]
    public void PlaylistModel_CanAddSongs()
    {
        // Arrange
        var songId1 = ObjectId.GenerateNewId();
        var songId2 = ObjectId.GenerateNewId();

        // Act
        _realm.Write(() =>
        {
            var playlist = new PlaylistModel
            {
                Id = ObjectId.GenerateNewId(),
                PlaylistName = "My Playlist",
                IsSmartPlaylist = false,
                DateCreated = DateTimeOffset.UtcNow
            };
            playlist.SongsIdsInPlaylist.Add(songId1);
            playlist.SongsIdsInPlaylist.Add(songId2);
            _realm.Add(playlist);
        });

        // Assert
        var savedPlaylist = _realm.All<PlaylistModel>().FirstOrDefault();
        Assert.NotNull(savedPlaylist);
        Assert.Equal(2, savedPlaylist.SongsIdsInPlaylist.Count);
        Assert.Contains(songId1, savedPlaylist.SongsIdsInPlaylist);
        Assert.Contains(songId2, savedPlaylist.SongsIdsInPlaylist);
    }

    [Fact]
    public void SongModel_CanHaveUserNotes()
    {
        // Arrange
        var noteText = "My favorite song!";

        // Act
        _realm.Write(() =>
        {
            var song = new SongModel
            {
                Id = ObjectId.GenerateNewId(),
                Title = "Test Song",
                ArtistName = "Test Artist",
                AlbumName = "Test Album",
                FilePath = "/test/path.mp3"
            };
            song.UserNotes.Add(new UserNoteModel
            {
                UserMessageText = noteText,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow
            });
            _realm.Add(song);
        });

        // Assert
        var savedSong = _realm.All<SongModel>().FirstOrDefault();
        Assert.NotNull(savedSong);
        Assert.Single(savedSong.UserNotes);
        Assert.Equal(noteText, savedSong.UserNotes.First().UserMessageText);
    }

    [Fact]
    public void MultipleSongs_CanShareSameNote()
    {
        // Arrange
        var sharedNote = "Road trip songs";

        // Act
        _realm.Write(() =>
        {
            var song1 = new SongModel
            {
                Id = ObjectId.GenerateNewId(),
                Title = "Song 1",
                ArtistName = "Artist 1",
                AlbumName = "Album 1",
                FilePath = "/test/song1.mp3"
            };
            song1.UserNotes.Add(new UserNoteModel { UserMessageText = sharedNote });

            var song2 = new SongModel
            {
                Id = ObjectId.GenerateNewId(),
                Title = "Song 2",
                ArtistName = "Artist 2",
                AlbumName = "Album 2",
                FilePath = "/test/song2.mp3"
            };
            song2.UserNotes.Add(new UserNoteModel { UserMessageText = sharedNote });

            _realm.Add(song1);
            _realm.Add(song2);
        });

        // Assert
        var songsWithNote = _realm.All<SongModel>()
            .Where(s => s.UserNotes.Any(n => n.UserMessageText == sharedNote))
            .ToList();

        Assert.Equal(2, songsWithNote.Count);
        Assert.All(songsWithNote, song => 
            Assert.Contains(song.UserNotes, note => note.UserMessageText == sharedNote)
        );
    }

    [Fact]
    public void NoteBasedPlaylist_ConceptValidation()
    {
        // Arrange
        var noteText = "Workout playlist";
        var expectedPlaylistName = $"Note: {noteText}";

        // Act - Simulate what SyncPlaylistFromUserNote does
        _realm.Write(() =>
        {
            // Create songs with the same note
            var song1 = new SongModel
            {
                Id = ObjectId.GenerateNewId(),
                Title = "High Energy",
                ArtistName = "Artist 1",
                AlbumName = "Album 1",
                FilePath = "/test/song1.mp3"
            };
            song1.UserNotes.Add(new UserNoteModel { UserMessageText = noteText });

            var song2 = new SongModel
            {
                Id = ObjectId.GenerateNewId(),
                Title = "Pump It Up",
                ArtistName = "Artist 2",
                AlbumName = "Album 2",
                FilePath = "/test/song2.mp3"
            };
            song2.UserNotes.Add(new UserNoteModel { UserMessageText = noteText });

            _realm.Add(song1);
            _realm.Add(song2);

            // Create a playlist for these songs
            var playlist = new PlaylistModel
            {
                Id = ObjectId.GenerateNewId(),
                PlaylistName = expectedPlaylistName,
                Description = $"Auto-generated playlist for songs with note: {noteText}",
                IsSmartPlaylist = false,
                DateCreated = DateTimeOffset.UtcNow
            };
            playlist.SongsIdsInPlaylist.Add(song1.Id);
            playlist.SongsIdsInPlaylist.Add(song2.Id);

            _realm.Add(playlist);
        });

        // Assert
        var noteBasedPlaylist = _realm.All<PlaylistModel>()
            .FirstOrDefault(p => p.PlaylistName == expectedPlaylistName);

        Assert.NotNull(noteBasedPlaylist);
        Assert.Equal(2, noteBasedPlaylist.SongsIdsInPlaylist.Count);
        Assert.StartsWith("Note:", noteBasedPlaylist.PlaylistName);
        Assert.Contains("Auto-generated", noteBasedPlaylist.Description);

        // Verify the songs in the playlist actually have the note
        var songsInPlaylist = _realm.All<SongModel>()
            .Where(s => noteBasedPlaylist.SongsIdsInPlaylist.Contains(s.Id))
            .ToList();

        Assert.All(songsInPlaylist, song =>
            Assert.Contains(song.UserNotes, note => note.UserMessageText == noteText)
        );
    }

    public void Dispose()
    {
        _realm?.Dispose();
        
        // Clean up test database file
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
