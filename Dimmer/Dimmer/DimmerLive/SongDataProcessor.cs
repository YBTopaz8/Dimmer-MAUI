using System.Threading.Tasks.Dataflow;


namespace Dimmer.DimmerLive;


//// A simple class to report progress
//public class LyricsProcessingProgress
//{
//    public int ProcessedCount { get; set; }
//    public int TotalCount { get; set; }
//    public string? CurrentFile { get; set; }
//    public bool FoundLyrics { get; set; }
//    public bool FoundUnsyncLyrics { get; set; }
//}


public class CoverProcessingProgress
{
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public string? CurrentFile { get; set; }
    public bool FoundCover { get; set; }
}
public class LyricsProcessingProgress
{
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public string? CurrentFile { get; set; }
    public bool FoundLyrics { get; set; }
    public bool FoundUnsyncLyrics { get; set; }
}

public static class SongDataProcessor
{
    public static async Task ProcessLyricsAsync(BaseViewModel vm,
        IRealmFactory RealmFactory,
        ILyricsMetadataService lyricsService,
        IProgress<LyricsProcessingProgress>? progress,
        CancellationToken cancellationToken)
    {
        var songs = RealmFactory.GetRealmInstance().All<SongModel>().Where(x=>!string.IsNullOrEmpty(x.SyncLyrics)).Freeze();
        var songList = songs;
        int totalCount = songs.Count();
        int processedCount = 0;

        // --- THE ACTION BLOCK ---
        // This is our asynchronous consumer. It will process items concurrently.
        var actionBlock = new ActionBlock<SongModel>(async song =>
        {
            try
            {

                // Operation was cancelled before we started this item.
                cancellationToken.ThrowIfCancellationRequested();


                // --- STEP 1: Fetch Lyrics from local file first ---
                Track track;
                var filePath = song.FilePath;
               
                    if (filePath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TaggingUtils.PlatformGetStreamHook != null)
                        {
                            using (var fileStream = TaggingUtils.PlatformGetStreamHook(filePath))
                            {

                                if (fileStream == null)
                                {                                   
                                    return ;
                                }
                                track = new ATL.Track(fileStream, mimeType: null);
                            }
                        }
                        else
                        {

                            return ;
                        }
                    }
                    else
                    {
                    // 3. Handle Standard Windows/File Paths
                     track = await Task.Run(() => new ATL.Track(filePath));
                }
                string? fetchedLrcData = track.Lyrics?.FirstOrDefault()?.FormatSynch();
                string? plainLyrics = track.Lyrics?.FirstOrDefault()?.UnsynchronizedLyrics;

                // --- STEP 2: If not found locally, search online ---
                if (string.IsNullOrWhiteSpace(fetchedLrcData) && string.IsNullOrWhiteSpace(plainLyrics))
                {
                    // Pass the cancellationToken to the service! This is crucial.
                    var onlineResults = await lyricsService.GetAllLyricsPropsOnlineAsync(song, cancellationToken);
                    var onlineResult = onlineResults?.FirstOrDefault(x=>!string.IsNullOrEmpty(x.SyncedLyrics));
                    if (onlineResult is not null)
                    {

                        var newSyncLyricsInfo = new LyricsInfo();
                        var newUnSyncLyrics = new LyricsInfo();
                        if (!string.IsNullOrWhiteSpace(fetchedLrcData))
                        {
                            newSyncLyricsInfo.Parse(fetchedLrcData);
                            newUnSyncLyrics.Parse(plainLyrics);
                        }
                        else
                        {
                            newUnSyncLyrics.UnsynchronizedLyrics = plainLyrics;
                        }

                        fetchedLrcData = onlineResult.SyncedLyrics;
                        plainLyrics = onlineResult.PlainLyrics;

                        // Save the new lyrics back to the file metadata
                        bool saved = await lyricsService.SaveLyricsToDB(onlineResult.Instrumental,plainLyrics, song, fetchedLrcData,newSyncLyricsInfo);
                        if (saved)
                        {


                            track.Lyrics ??= new List<LyricsInfo>();
                            track.Lyrics.Clear();
                            track.Lyrics.Add(newSyncLyricsInfo);
                            track.Lyrics.Add(newUnSyncLyrics);
                            if(vm.CurrentPlayingSongView.TitleDurationKey != song.TitleDurationKey)
                            {
                                await track.SaveAsync();
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {

                // This is expected, just let it bubble up to stop the block.
                throw new OperationCanceledException(ex.Message) ;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing lyrics for {song.FilePath}: {ex.Message}");
            }
            finally
            {
                // Report progress in a thread-safe way.
                int currentProcessed = Interlocked.Increment(ref processedCount);
                progress?.Report(new LyricsProcessingProgress
                {
                    ProcessedCount = currentProcessed,
                    TotalCount = totalCount,
                    CurrentFile = Path.GetFileName(song.FilePath),
                    FoundLyrics = !string.IsNullOrEmpty(song.SyncLyrics),
                    FoundUnsyncLyrics = !string.IsNullOrEmpty(song.UnSyncLyrics)
                });
            }
        },
        // --- CONFIGURATION ---
        new ExecutionDataflowBlockOptions
        {
            // This is the magic. Run up to 3 tasks in parallel. Perfect for network I/O.
            MaxDegreeOfParallelism = 3,
            CancellationToken = cancellationToken
        });

        // --- THE PRODUCER ---
        // This is now a simple loop that "posts" work to the block. It doesn't block.
        foreach (var song in songList)
        {
            await actionBlock.SendAsync(song, cancellationToken);
        }

        // 1. Tell the block we are done adding items.
        actionBlock.Complete();

        // 2. Asynchronously wait for all queued items to be processed.
        try
        {
            // By passing the token to WaitAsync, we allow the outer 
            // await to instantly cancel, rather than waiting for the block to slowly wind down.
            await actionBlock.Completion.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Optional: Log that the user aborted the mass scan.
            // Do not throw here if you want the method to exit gracefully.
        }
    }
}