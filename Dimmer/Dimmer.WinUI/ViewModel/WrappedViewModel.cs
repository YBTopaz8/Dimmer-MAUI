using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Utils;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

namespace Dimmer.WinUI.ViewModel;


public partial class WrappedViewModel : ObservableObject
{
    private readonly ListeningReportGenerator _generator;

    public WrappedViewModel(ListeningReportGenerator generator)
    {
        _generator = generator;
    }

    // --- PAGE 1: TOP SONG ---
    [ObservableProperty] private SongModelView? _topSong;
    [ObservableProperty] private string _topSongPlayCount;
    [ObservableProperty] private string _topSongDuration;
    [ObservableProperty] private ISeries[] _topSongDailyChart; // Specific chart for the top song

    // --- PAGE 2: ARTIST & ALBUM ---
    [ObservableProperty] private ArtistModelView? _topArtist;
    [ObservableProperty] private AlbumModelView? _topAlbum;
    [ObservableProperty] private ObservableCollection<DimmerStats> _top5Artists;
    [ObservableProperty] private string _topArtistHours;

    // --- PAGE 3: SUMMARY ---
    [ObservableProperty] private string _totalMinutes;
    [ObservableProperty] private string _totalSongs;
    [ObservableProperty] private string _musicPersonality; // e.g., "The Explorer"
    [ObservableProperty] private ISeries[] _genreChart;

    public async Task InitializeAsync()
    {
        // 1. Ensure Report is Generated (Assume GenerateReportAsync was called with a Date Range like "Last Year")

        // --- POPULATE PAGE 1 (Song) ---
        var topTracks = _generator.GetTopTracks();
        if (topTracks != null && topTracks.Any())
        {
            var best = topTracks.First();
            TopSong = best.Song;
            TopSongPlayCount = $"{best.Count} plays";

            // Calculate duration (Count * Duration)
            TimeSpan t = TimeSpan.FromSeconds((long)(best.Song.DurationInSeconds * best.Count));
            TopSongDuration = $"{t.TotalHours:F1} hours";

            // Create a "Vibe" chart (Smoothed line, no axes visible)
            TopSongDailyChart = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new double[] { 2, 5, 4, 6, 8, 12, 15, 10, 8, 5 }, // Placeholder: Get actual daily history for this specific song ID if possible
                    Fill = new SolidColorPaint(SKColors.White.WithAlpha(30)),
                    Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 4 },
                    GeometrySize = 0,
                    LineSmoothness = 1
                }
            };
        }

        // --- POPULATE PAGE 2 (Artist/Album) ---
        var topArtists = _generator.GetTopArtists();
        if (topArtists != null && topArtists.Any())
        {
            TopArtist = topArtists.First().SongArtist;
            Top5Artists = new ObservableCollection<DimmerStats>(topArtists.Take(5));
        }

        var topAlbums = _generator.GetTopAlbums();
        if (topAlbums != null && topAlbums.Any())
        {
            TopAlbum = topAlbums.First().SongAlbum;
        }

        // --- POPULATE PAGE 3 (Summary) ---
        var timeStats = _generator.GetTotalListeningTime();
        TotalMinutes = timeStats != null ? $"{timeStats.Value} Hours" : "0 Hours";

        var uniqueTracks = _generator.GetUniqueTracks();
        TotalSongs = uniqueTracks != null ? $"{uniqueTracks.Count}" : "0";

        // Calculate "Personality" based on Discovery Rate
        var discovery = _generator.GetDiscoveryRate();
        MusicPersonality = (discovery?.Value ?? 0) > 30 ? "The Explorer" : "The Loyalist";

        // Genre Pie Chart
        var genres = _generator.GetTopGenres()?.Take(4);
        if (genres != null)
        {
            GenreChart = genres.Select(g => new PieSeries<double>
            {
                Values = new double[] { (double)g.Count },
                Name = g.StatTitle,
                InnerRadius = 60
            }).ToArray();
        }
    }
}