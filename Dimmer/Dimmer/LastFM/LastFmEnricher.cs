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
        var lookup = new Dictionary<string, SongModelView>();

        foreach (var song in localSongs)
        {
            if (string.IsNullOrEmpty(song.Title) || string.IsNullOrEmpty(song.ArtistName))
                continue;

            // 1. Index the Exact Match (e.g., "Get Lucky|Daft Punk | Pharrell")
            var exactKey = GenerateKey(song.Title, song.ArtistName);
            lookup.TryAdd(exactKey, song);

            // 2. Index the "Scrobble Friendly" Match (e.g., "Get Lucky|Daft Punk")
            // Check if the artist has your specific separator "| "
            if (song.ArtistName.Contains("|"))
            {
                var splitArtist = song.ArtistName.Split('|', StringSplitOptions.TrimEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(splitArtist))
                {
                    var splitKey = GenerateKey(song.Title, splitArtist);
                    // TryAdd ensures we don't crash if "Daft Punk" entry already exists
                    lookup.TryAdd(splitKey, song);
                }
            }
        }

        return lookup;
    }

    // Overload for Tracks (Recent, Top, Loved)
    public static IEnumerable<Track> EnrichWithLocalData(this IEnumerable<Track> tracks,
        Dictionary<string, SongModelView> primaryLookup,
        IEnumerable<SongModelView> allSongsForFallback)
    {
        foreach (var track in tracks)
        {
            SongModelView? match = null;

            var key = GenerateKey(track.Name, track.Artist?.Name);

            if (!string.IsNullOrEmpty(key) && primaryLookup.TryGetValue(key, out var exactMatch))
            {
                match = exactMatch;
            }

            else
            {
                double targetDuration = track.Duration * 0.001; // Convert ms to seconds

                if (targetDuration > 0)
                {
                    // We rely on the loose tolerance logic here
                    match = allSongsForFallback.FirstOrDefault(s =>
                        s.Title.Equals(track.Name, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(s.DurationInSeconds - targetDuration) < 3); // Increased tolerance slightly to 3s
                }
            }

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
    public static Dictionary<string, AlbumModel> BuildLocalAlbumLookup(IEnumerable<AlbumModel> localAlbums)
    {
        var lookup = new Dictionary<string, AlbumModel>();

        foreach (var album in localAlbums)
        {
            // Safe checks because Realm objects can have null links
            var albumName = album.Name;
            var artistName = album.Artist?.Name;

            if (string.IsNullOrEmpty(albumName) || string.IsNullOrEmpty(artistName))
                continue;

            // 1. Index Exact Match
            var exactKey = GenerateKey(albumName, artistName);
            lookup.TryAdd(exactKey, album);

            // 2. Index Split Artist (e.g. "Daft Punk" from "Daft Punk | Pharrell")
            if (artistName.Contains("|"))
            {
                var splitArtist = artistName.Split('|', StringSplitOptions.TrimEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(splitArtist))
                {
                    var splitKey = GenerateKey(albumName, splitArtist);
                    lookup.TryAdd(splitKey, album);
                }
            }
        }

        return lookup;
    }
    // Overload for Albums
    public static IEnumerable<Hqub.Lastfm.Entities.Album> EnrichWithLocalData(
    this IEnumerable<Hqub.Lastfm.Entities.Album> lastFmAlbums,
    IQueryable<AlbumModel> realmQuery)
    {
        foreach (var lfmAlbum in lastFmAlbums)
        {
            var lfmTitle = lfmAlbum.Name;
            var lfmArtist = lfmAlbum.Artist?.Name;

            if (string.IsNullOrEmpty(lfmTitle) || string.IsNullOrEmpty(lfmArtist))
            {
                lfmAlbum.IsOnPresentDevice = false;
                yield return lfmAlbum;
                continue;
            }
            var potentialMatches = realmQuery
                .Filter("Name ==[c] $0", lfmTitle)
                .ToList(); // Execute immediately to get the small list into memory

            bool isMatch = false;

            // 2. IN-MEMORY CHECK (Safe C# logic)
            // Now that we have the 1 or 2 albums with that name, we check the artist in C#
            foreach (var localAlbum in potentialMatches)
            {
                var localArtist = localAlbum.Artists.FirstOrDefault()?.Name;

                if (string.IsNullOrEmpty(localArtist)) continue;

                // Check A: Simple Match (using C# StringComparison now that we are in memory)
                if (string.Equals(localArtist, lfmArtist, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                    break;
                }

                // Check B: Split Artist Logic
                // Local: "Daft Punk | Pharrell" vs LFM: "Daft Punk"
                if (localArtist.Contains("|"))
                {
                    // We can do complex C# splits here safely
                    var firstPart = localArtist.Split('|', StringSplitOptions.TrimEntries).FirstOrDefault();
                    if (string.Equals(firstPart, lfmArtist, StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = true;
                        break;
                    }
                }
            }

            lfmAlbum.IsOnPresentDevice = isMatch;
            yield return lfmAlbum;
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
        // Use a separator that is unlikely to appear in song names (|||)
        return $"{title.Trim().ToLowerInvariant()}|{artist.Trim().ToLowerInvariant()}";
    }
}