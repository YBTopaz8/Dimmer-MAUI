using ATL;
using ATL;

using Dimmer.Interfaces.Services.Interfaces;

using System;

using System;
using System.Collections.Concurrent;
using System.Collections.Concurrent; // For BlockingCollection
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;


namespace Dimmer.DimmerLive;


// A simple class to report progress
public class LyricsProcessingProgress
{
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public string? CurrentFile { get; set; }
}

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
        IProgress<LyricsProcessingProgress> progress,
        CancellationToken cancellationToken)
    {
        // Use ToList() to avoid issues with collection modification during enumeration
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
                         var track = new Track(song.FilePath);
                         if (!string.IsNullOrWhiteSpace(track.Lyrics?.First().UnsynchronizedLyrics) || (track.Lyrics?.First().SynchronizedLyrics?.Any() ?? false))
                         {
                             // If found, format it into LRC data string.
                             fetchedLrcData = track.Lyrics.First().FormatSynch();
                         }
                         else
                         {
                             //continue; // No lyrics found in metadata, skip to next song.
                             // If not found in file, use your service to search online.
                             var onlineResults = await lyricsService.SearchOnlineAsync(song);
                             fetchedLrcData = onlineResults?.FirstOrDefault()?.SyncedLyrics;
                         }

                         // --- STEP 3: If lyrics were found, process and save them ---
                         if (!string.IsNullOrWhiteSpace(fetchedLrcData))
                         {
                             // A. Parse the LRC data into ATL's structure
                             var newLyricsInfo = new LyricsInfo();
                             newLyricsInfo.Parse(fetchedLrcData);

                             // B. Save the fetched lyrics BACK TO THE FILE'S METADATA

                             bool saved = await lyricsService.SaveLyricsForSongAsync(song, fetchedLrcData, newLyricsInfo);
                             if (saved)
                             {
                                 // C. Update the UI model on the main thread
                                 MainThread.BeginInvokeOnMainThread(() =>
                                 {
                                     song.HasLyrics = !string.IsNullOrWhiteSpace(newLyricsInfo.UnsynchronizedLyrics);
                                     song.HasSyncedLyrics = newLyricsInfo.SynchronizedLyrics.Any();
                                     song.UnSyncLyrics = newLyricsInfo.UnsynchronizedLyrics;
                                     song.EmbeddedSync.Clear(); // Ensure it's empty before adding
                                     song.SyncLyrics= newLyricsInfo.SynchronizedLyrics.Any() ? newLyricsInfo.SynchronizedLyrics.ToString()! : string.Empty;
                                     foreach (var phrase in newLyricsInfo.SynchronizedLyrics)
                                     {
                                         song.EmbeddedSync.Add(new SyncLyricsView(phrase));
                                     }
                                 });
                                 var fileToUpdate = new Track(song.FilePath);
                                 fileToUpdate.Lyrics.Add(newLyricsInfo);
                                 fileToUpdate.Save(); // Persist the changes!


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
                         });
                     }
                 }
             }, cancellationToken));
        }

        return Task.WhenAll(consumerTasks.Concat(new[] { producerTask }));
    }
}