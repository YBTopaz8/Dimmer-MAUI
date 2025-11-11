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
    public static async Task ProcessLyricsAsync(
        IRealmFactory RealmFactory,
        IReadOnlyCollection<SongModel> songsToProcess,
        ILyricsMetadataService lyricsService,
        IProgress<LyricsProcessingProgress>? progress,
        CancellationToken cancellationToken)
    {
        var songList = songsToProcess;
        int totalCount = songList.Count;
        int processedCount = 0;

        // --- THE ACTION BLOCK ---
        // This is our asynchronous consumer. It will process items concurrently.
        var actionBlock = new ActionBlock<SongModel>(async song =>
        {
            try
            {
                // Operation was cancelled before we started this item.
                cancellationToken.ThrowIfCancellationRequested();

                // Skip if lyrics are already present.
                if (!string.IsNullOrEmpty(song.SyncLyrics))
                {
                    return;
                }

                // --- STEP 1: Fetch Lyrics from local file first ---
                var track = new Track(song.FilePath);
                string? fetchedLrcData = track.Lyrics?.FirstOrDefault()?.FormatSynch();
                string? plainLyrics = track.Lyrics?.FirstOrDefault()?.UnsynchronizedLyrics;

                // --- STEP 2: If not found locally, search online ---
                if (string.IsNullOrWhiteSpace(fetchedLrcData) && string.IsNullOrWhiteSpace(plainLyrics))
                {
                    // Pass the cancellationToken to the service! This is crucial.
                    var onlineResults = await lyricsService.SearchLyricsAsync(song.Title,song.ArtistName,song.AlbumName, cancellationToken);
                    var onlineResult = onlineResults.FirstOrDefault();
                    if (onlineResult is not null)
                    {

                        var newLyricsInfo = new LyricsInfo();
                        if (!string.IsNullOrWhiteSpace(fetchedLrcData))
                        {
                            newLyricsInfo.Parse(fetchedLrcData);
                        }
                        else
                        {
                            newLyricsInfo.UnsynchronizedLyrics = plainLyrics;
                        }

                        fetchedLrcData = onlineResult.SyncedLyrics;
                        plainLyrics = onlineResult.PlainLyrics;

                        // Save the new lyrics back to the file metadata
                        bool saved = await lyricsService.SaveLyricsToDB(onlineResult.Instrumental,plainLyrics, song, fetchedLrcData,newLyricsInfo);
                        if (saved)
                        {

                            // Update the UI model on the main thread
                            //MainThread.BeginInvokeOnMainThread(() =>
                            //{
                            //    song.HasLyrics = !string.IsNullOrWhiteSpace(plainLyrics);
                            //    song.UnSyncLyrics = plainLyrics;
                            //    song.SyncLyrics = fetchedLrcData; // Or however you store it
                            //});
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
            // This is the magic. Run up to 4 tasks in parallel. Perfect for network I/O.
            MaxDegreeOfParallelism = 4,
            CancellationToken = cancellationToken
        });

        // --- THE PRODUCER ---
        // This is now a simple loop that "posts" work to the block. It doesn't block.
        foreach (var song in songList)
        {
            await actionBlock.SendAsync(song, cancellationToken);
        }

        // --- WAIT FOR COMPLETION ---
        // 1. Tell the block we are done adding items.
        actionBlock.Complete();
        // 2. Asynchronously wait for all queued items to be processed.
        await actionBlock.Completion;
    }
}
/*
public static class SongDataProcessor
{
    /// <summary>
    /// Processes a collection of songs on background threads to extract and update their lyrics information.
    /// This method is non-blocking and uses progress reporting for UI updates.
    /// </summary>
    /// <param name="songsToProcess">The enumerable of songs to process.</param>
    /// <param name="progress">An IProgress object to report progress back to the UI thread.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A Task that completes when all processing is finished.</returns>
    public static Task ProcessLyricsAsync(
        IEnumerable<SongModelView> songsToProcess, ILyricsMetadataService lyricsService,
        IProgress<LyricsProcessingProgress>? progress,
        CancellationToken cancellationToken)
    {
       
        var songList = songsToProcess.ToList();
        int totalCount = songList.Count;
        int processedCount = 0;

        // The Producer/Consumer pattern is excellent for I/O-bound work.
        // It prevents overwhelming the disk with too many simultaneous file reads.
        var queue = new BlockingCollection<SongModelView>();

        // --- THE PRODUCER ---
        // This task's only job is to quickly add all songs to the processing queue.
        var producerTask = Task.Run(() =>
        {
            try
            {
                foreach (var song in songList)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    // Add song to the queue. If the queue is full, it will wait.
                    queue.Add(song, cancellationToken);
                }
            }
            finally
            {
                // Signal that no more items will be added.
                queue.CompleteAdding();
            }
        });

        // --- THE CONSUMERS ---
        // We'll create a few consumer tasks to process files concurrently.
        // 2-4 is a good number for disk I/O to avoid thrashing.
        int consumerCount = Math.Min(Environment.ProcessorCount, 3);
        var consumerTasks = new List<Task>();

        for (int i = 0; i < consumerCount; i++)
        {
            consumerTasks.Add(Task.Run(async () =>
             {
                 foreach (var song in queue.GetConsumingEnumerable(cancellationToken))
                 {
                     try
                     {

                         // --- STEP 1: Check if processing is even needed ---
                         // If the song already has lyrics loaded in the model, we can skip it.
                         if (song.SyncLyrics.Length>1)
                         {
                             // We still report progress for a responsive UI.

                             continue;
                         }

                         // --- STEP 2: Fetch Lyrics (combining your old and new logic) ---
                         string? fetchedLrcData = null;

                         // First, try to get from local file metadata using ATL (our original fast path)
                         var track = new ATL.Track(song.FilePath);
                         song.AlbumName = track.Album;
                         song.ArtistName= track.Artist;
                         song.OtherArtistsName= track.OriginalArtist;
                         song.GenreName=track.Genre;
                        
                         
                         if (track.Lyrics?.Count>0)
                         { 
                             if (!string.IsNullOrWhiteSpace(track.Lyrics?.First().UnsynchronizedLyrics) || (track.Lyrics?.First().SynchronizedLyrics?.Any() ?? false))
                             {
                                 // If found, format it into LRC data string.
                                 fetchedLrcData = track.Lyrics.First().FormatSynch();
                                 if (string.IsNullOrEmpty(fetchedLrcData))
                                 {

                                     var onlineResults = await lyricsService.SearchOnlineAsync(song);
                                     if (onlineResults is not null && onlineResults.Any())
                                     {
                                         var firstFetched = onlineResults.First();

                                         fetchedLrcData = firstFetched.SyncedLyrics;
                                         song.IsInstrumental = firstFetched.Instrumental;
                                         song.UnSyncLyrics=firstFetched.PlainLyrics;

                                     }
                                 }
                             }
                         }
                         else
                         {
                             //continue; // No lyrics found in metadata, skip to next song.
                             // If not found in file, use your service to search online.
                             var onlineResults = await lyricsService.SearchOnlineAsync(song);
                             if (onlineResults is not null && onlineResults.Any())
                             {
                                 var firstFetched = onlineResults.First();
                                
                                     fetchedLrcData = firstFetched.SyncedLyrics;
                                     song.IsInstrumental = firstFetched.Instrumental;
                                 song.UnSyncLyrics=firstFetched.PlainLyrics;
                                 
                             }
                         }

                         // --- STEP 3: If lyrics were found, process and save them ---
                         if (!string.IsNullOrWhiteSpace(fetchedLrcData))
                         {
                             // A. Parse the LRC data into ATL's structure
                             var newLyricsInfo = new LyricsInfo();
                             newLyricsInfo.Parse(fetchedLrcData);

                             // B. Save the fetched lyrics BACK TO THE FILE'S METADATA

                             bool saved = await lyricsService.SaveLyricsForSongAsync(false, song.UnSyncLyrics, song, fetchedLrcData, newLyricsInfo);
                             if (saved)
                             {
                                 // C. Update the UI model on the main thread
                                 MainThread.BeginInvokeOnMainThread(() =>
                                 {
                                     song.HasLyrics = !string.IsNullOrWhiteSpace(newLyricsInfo.UnsynchronizedLyrics);

                                     song.UnSyncLyrics = newLyricsInfo.UnsynchronizedLyrics;
                                     song.EmbeddedSync.Clear(); // Ensure it's empty before adding
                                     song.SyncLyrics= newLyricsInfo.SynchronizedLyrics.Any() ? newLyricsInfo.SynchronizedLyrics.ToString()! : string.Empty;
                                     foreach (var phrase in newLyricsInfo.SynchronizedLyrics)
                                     {
                                         song.EmbeddedSync.Add(new LyricPhraseModelView(phrase));
                                     }
                                 });
                                 
                                 //var fileToUpdate = new ATL.Track(song.FilePath);
                                 //fileToUpdate.Lyrics.Add(newLyricsInfo);
                                 //fileToUpdate.Save(); // Persist the changes!


                             }
                         }
                     }
                     catch (Exception ex)
                     {
                         Debug.WriteLine($"Error processing lyrics for {song.FilePath}: {ex.Message}");
                     }
                     finally
                     {
                         // Report progress regardless of success or failure.
                         int currentProcessed = Interlocked.Increment(ref processedCount);
                         progress?.Report(new LyricsProcessingProgress
                         {
                             ProcessedCount = currentProcessed,
                             TotalCount = totalCount,
                             CurrentFile = Path.GetFileName(song.FilePath)
                             ,FoundLyrics = song.HasSyncedLyrics
                             ,FoundUnsyncLyrics = song.HasLyrics
                         });
                     }
                 }
             }, cancellationToken));
        }

        return Task.WhenAll(consumerTasks.Concat(new[] { producerTask }));
    }
}

*/