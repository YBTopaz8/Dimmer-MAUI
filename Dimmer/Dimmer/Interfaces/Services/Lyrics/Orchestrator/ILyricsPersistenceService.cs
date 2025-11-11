namespace Dimmer.Interfaces.Services.Lyrics.Orchestrator;

public class LyricsPersistenceService : ILyricsPersistenceService
{
    private readonly IRealmFactory _realmFactory;
    private readonly ILogger<LyricsPersistenceService> _logger;

    public LyricsPersistenceService(IRealmFactory realmFactory, ILogger<LyricsPersistenceService> logger)
    {
        _realmFactory = realmFactory;
        _logger = logger;
    }

    // This is your "lyrics bank" read method
    public Task<string?> GetCachedLyricsAsync(SongModelView song)
    {
        using var realm = _realmFactory.GetRealmInstance();
        var songModel = realm.Find<SongModel>(song.Id);

        // Return cached lyrics if they exist
        if (songModel != null && songModel.HasSyncedLyrics && !string.IsNullOrEmpty(songModel.SyncLyrics))
        {
            _logger.LogInformation("Found cached lyrics in database for '{Title}'", song.Title);
            return Task.FromResult<string?>(songModel.SyncLyrics);
        }
        return Task.FromResult<string?>(null);
    }

    public async Task<bool> SaveLyricsAsync(SongModelView song, string lrcContent)
    {
        if (string.IsNullOrEmpty(song?.FilePath) || string.IsNullOrWhiteSpace(lrcContent))
            return false;

        bool dbSuccess = await SaveToDatabaseAsync(song.Id, lrcContent);
        // You can decide if you also want to save to a local .lrc file or embed it.
        // For a caching system, saving to the DB is often sufficient and cleaner.
        // bool fileSuccess = await SaveToLrcFileAsync(song.FilePath, lrcContent);

        return dbSuccess;
    }

    private async Task<bool> SaveToDatabaseAsync(ObjectId songId, string lrcContent)
    {
        try
        {
            using var realm = _realmFactory.GetRealmInstance();
            var songModel = realm.Find<SongModel>(songId);
            if (songModel == null)
                return false;

            var newLyricsInfo = new LyricsInfo();
            newLyricsInfo.Parse(lrcContent);

            await realm.WriteAsync(() =>
            {
                songModel.SyncLyrics = lrcContent;
                songModel.HasSyncedLyrics = newLyricsInfo.SynchronizedLyrics.Any();
                songModel.HasLyrics = true;
                songModel.EmbeddedSync.Clear();

                foreach (var line in newLyricsInfo.SynchronizedLyrics)
                {
                    songModel.EmbeddedSync.Add(new SyncLyrics { TimestampMs = line.TimestampStart, Text = line.Text });
                }
                songModel.UnSyncLyrics = newLyricsInfo.UnsynchronizedLyrics;
                songModel.LastDateUpdated = DateTimeOffset.UtcNow;
            });

            _logger.LogInformation("Successfully saved lyrics to database for song {Id}", songId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save lyrics to database for song {Id}", songId);
            return false;
        }
    }
}