using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hqub.Lastfm.Entities;

using Track = Hqub.Lastfm.Entities.Track;

namespace Dimmer.LastFM;

public static class LastFmEnricher
{
    // We create a "Lookup" to make searching instantaneous (O(1))
    // Key: "Title|Artist", Value: The SongModelView
    public static Dictionary<string, SongModelView> BuildLocalLibraryLookup(IEnumerable<SongModelView> localSongs)
    {
        // We use a safe dictionary approach in case of duplicate songs
        return localSongs
            .Where(s => !string.IsNullOrEmpty(s.Title) && !string.IsNullOrEmpty(s.ArtistName))
            .GroupBy(s => GenerateKey(s.Title, s.ArtistName)) // Handle duplicates
            .ToDictionary(g => g.Key, g => g.First());
    }

    // Overload for Tracks (Recent, Top, Loved)
    public static IEnumerable<Track> EnrichWithLocalData(this IEnumerable<Track> tracks,
        Dictionary<string, SongModelView> primaryLookup,
        IEnumerable<SongModelView> allSongsForFallback)
    {
        foreach (var track in tracks)
        {
            SongModelView? match = null;

            // 1. Primary Check: O(1) Lookup by Title & Artist
            var key = GenerateKey(track.Name, track.Artist?.Name);
            if (primaryLookup.TryGetValue(key, out var exactMatch))
            {
                match = exactMatch;
            }
            // 2. Secondary Check: Fallback to Duration (Slower, but only runs if primary fails)
            else
            {
                double targetDuration = track.Duration * 0.001;
                // Only search if we have a valid duration to match against
                if (targetDuration > 0)
                {
                    match = allSongsForFallback.FirstOrDefault(s =>
                        s.Title.Equals(track.Name, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(s.DurationInSeconds - targetDuration) < 2); // 2 second tolerance
                }
            }

            // Apply the logic
            if (match != null)
            {
                ApplyLocalDataToTrack(track, match);
            }
            else
            {
                track.IsOnPresentDevice = false;
            }

            yield return track;
        }
    }

    // Overload for Albums
    public static IEnumerable<Album> EnrichWithLocalData(this IEnumerable<Album> albums,
        Dictionary<string, SongModelView> primaryLookup)
    {
        foreach (var album in albums)
        {
            

            album.IsOnPresentDevice = false; // Default
            yield return album;
        }
    }

    private static void ApplyLocalDataToTrack(Track track, SongModelView localSong)
    {
        track.IsOnPresentDevice = true;
        track.OnDeviceObjectId = localSong.Id.ToString();

        if (track.Album == null && !string.IsNullOrEmpty(localSong.AlbumName))
        {
            track.Album = new Album { Name = localSong.AlbumName };
        }

        // Inject Image
        if (!string.IsNullOrEmpty(localSong.CoverImagePath))
        {
            // Ensure lists exists
            if (track.Images == null) track.Images = new List<Hqub.Lastfm.Entities.Image>();

            var localImage = new Hqub.Lastfm.Entities.Image
            {
                Size = "extralarge",
                Url = localSong.CoverImagePath
            };

            // Your original logic inserted at 3. We ensure we don't crash if count < 3
            if (track.Images.Count >= 3)
                track.Images.Insert(3, localImage);
            else
                track.Images.Add(localImage);
        }
    }

    // Helper to generate consistent keys (Case insensitive, trimmed)
    private static string GenerateKey(string? title, string? artist)
    {
        return $"{title?.Trim().ToLowerInvariant()}|{artist?.Trim().ToLowerInvariant()}";
    }
}