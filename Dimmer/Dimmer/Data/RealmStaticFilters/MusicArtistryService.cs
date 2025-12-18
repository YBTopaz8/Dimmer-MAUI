namespace Dimmer.Data.RealmStaticFilters;

public class MusicArtistryService
{
    private Realm _realm;
    private static readonly double GoldenRatio = 1.61803398875;

    public MusicArtistryService(IRealmFactory factory)
    {
        _realm = factory.GetRealmInstance();
    }

    public record FibonacciDiscovery(ArtistModel Artist, int DiscoveryDay, int FibonacciNumber);
    public record GoldenRatioTrack(SongModel Song, string Reason);

    #region 1. Mathematical & Sequence-Based Insights

    /// <summary>
    /// Q: "Find my 'Fibonacci Artists' - artists I discovered on a Fibonacci-sequence day after I started listening."
    /// A quirky stat that connects a user's discovery timeline to a famous mathematical sequence.
    /// </summary>
    public List<FibonacciDiscovery> GetFibonacciArtists()
    {
        var firstEverPlay = _realm.All<DimmerPlayEvent>().OrderBy(p => p.DatePlayed).FirstOrDefault();
        if (firstEverPlay == null)
            return new List<FibonacciDiscovery>();

        var startDate = firstEverPlay.DatePlayed;
        var fibonacciDays = new HashSet<int> { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377 };

        var artistFirstPlayDates = _realm.All<DimmerPlayEvent>().ToList()
            .Where(p => p.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(p => p.SongsLinkingToThisEvent.First().Artist)
            .Select(g => new { Artist = g.Key, FirstPlay = g.Min(p => p.DatePlayed) })
            .ToList();

        var results = new List<FibonacciDiscovery>();
        foreach (var item in artistFirstPlayDates)
        {
            int daysSinceStart = (int)(item.FirstPlay - startDate).TotalDays;
            if (fibonacciDays.Contains(daysSinceStart))
            {
                results.Add(new FibonacciDiscovery(item.Artist, daysSinceStart, daysSinceStart));
            }
        }
        return [.. results.OrderBy(r => r.DiscoveryDay)];
    }

    /// <summary>
    /// Q: "What's the 'Golden Ratio' track on my favorite album?"
    /// Finds the track on the user's most-played album that is closest to the Golden Ratio point (approx 61.8%) of the album's total duration.
    /// This is often considered a point of high aesthetic interest in compositions.
    /// </summary>
    public GoldenRatioTrack? GetGoldenRatioTrackOnFavoriteAlbum()
    {

        // 1. Find the most played album
        var topAlbum = _realm.All<DimmerPlayEvent>()
            .Filter("SongsLinkingToThisEvent.@count > 0").ToList()
            .Where(p => p.SongsLinkingToThisEvent.FirstOrDefault()?.Album != null)
            .GroupBy(p => p.SongsLinkingToThisEvent.First().Album)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        if (topAlbum == null || topAlbum.SongsInAlbum?.Count() == 0)
            return null;

        // 2. Calculate album duration and the Golden Ratio point
        var albumSongs = topAlbum.SongsInAlbum?.OrderBy(s => s.TrackNumber).ToList();
        double totalDuration = albumSongs.Sum(s => s.DurationInSeconds);
        double goldenRatioTimestamp = totalDuration / GoldenRatio;

        // 3. Find which song is playing at that timestamp
        double cumulativeDuration = 0;
        foreach (var song in albumSongs)
        {
            if (cumulativeDuration + song.DurationInSeconds >= goldenRatioTimestamp)
            {
                return new GoldenRatioTrack(song, $"The 'Golden Ratio' point of your top album, '{topAlbum.Name}'.");
            }
            cumulativeDuration += song.DurationInSeconds;
        }
        return null;
    }

    /// <summary>
    /// Q: "Are my listening sessions 'Power Law' distributed? (The 80/20 rule)"
    /// Checks if ~80% of plays come from ~20% of the artists.
    /// </summary>
    public (bool FollowsPowerLaw, double PlayPercentage, double ArtistPercentage) GetParetoPrincipleCheck()
    {

        var artistPlayCounts = _realm.All<DimmerPlayEvent>().ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist)
            .Select(g => g.Count())
            .OrderByDescending(c => c)
            .ToList();

        if (artistPlayCounts.Count == 0)
            return (false, 0, 0);

        long totalPlays = artistPlayCounts.Sum();
        int top20PercentArtistCount = (int)(artistPlayCounts.Count * 0.2);

        long topArtistPlays = artistPlayCounts.Take(top20PercentArtistCount).Sum();
        double playPercentage = (double)topArtistPlays / totalPlays;

        return (playPercentage >= 0.75 && playPercentage <= 0.85, playPercentage, 0.2);
    }

    /// <summary>
    /// Q: "Find the 'Prime Number' songs - songs I've played a prime number of times."
    /// </summary>
    public Dictionary<SongModel, int> GetPrimeNumberSongs()
    {

        var primeNumbers = new HashSet<int> { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 }; // etc.
        return _realm.All<SongModel>().ToList()
            .Select(s => new { Song = s, Plays = s.PlayHistory.Count })
            .Where(x => primeNumbers.Contains(x.Plays))
            .ToDictionary(x => x.Song, x => x.Plays);
    }

    #endregion

    #region 2. Artistic & Structural Pattern Recognition

    /// <summary>
    /// Q: "What's my 'Musical Trinity'? The three artists who form the vertices of my taste."
    /// Finds the three most-played artists who are from different "super-genres" (e.g., Rock, Electronic, Pop/R&B).
    /// </summary>
    public List<ArtistModel> GetMusicalTrinity()
    {

        var superGenres = new Dictionary<string, HashSet<string>>
        {
            { "Rock/Alternative", new HashSet<string> { "Rock", "Alternative", "Indie", "Metal", "Punk" } },
            { "Electronic", new HashSet<string> { "Electronic", "Techno", "House", "Trance", "Ambient" } },
            { "Pop/R&B/Hip-Hop", new HashSet<string> { "Pop", "R&B", "Hip-Hop", "Soul", "Funk" } },
            { "Classical/Jazz", new HashSet<string> { "Classical", "Jazz", "Blues" } }
        };

        var trinity = new List<ArtistModel>();
        var topArtists = _realm.All<DimmerPlayEvent>().ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist)
            .Select(g => new { Artist = g.Key, Plays = g.Count() })
            .OrderByDescending(x => x.Plays)
            .ToList();

        var assignedSuperGenres = new HashSet<string>();

        foreach (var artistStat in topArtists)
        {
            var artistTopGenre = artistStat.Artist.Songs
                .Select(s => s.Genre.Name)
                .Where(n => n != null)
                .GroupBy(n => n)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            if (artistTopGenre == null)
                continue;

            foreach (var superGenre in superGenres)
            {
                if (superGenre.Value.Contains(artistTopGenre) && !assignedSuperGenres.Contains(superGenre.Key))
                {
                    trinity.Add(artistStat.Artist);
                    assignedSuperGenres.Add(superGenre.Key);
                    if (trinity.Count == 3)
                        return trinity;
                    break;
                }
            }
        }
        return trinity;
    }

    /// <summary>
    /// Q: "Find my 'Perfect Pairings' - two songs by different artists I almost always listen to together."
    /// </summary>
    public List<(SongModel Song1, SongModel Song2, int Pairings)> GetPerfectPairings(TimeSpan maxTimeBetween)
    {

        var plays = _realm.All<DimmerPlayEvent>().OrderBy(p => p.DatePlayed).ToList();
        var pairings = new Dictionary<(ObjectId, ObjectId), int>();

        for (int i = 0; i < plays.Count - 1; i++)
        {
            var p1 = plays[i];
            var p2 = plays[i + 1];

            if (p1.SongId == p2.SongId || p1.SongsLinkingToThisEvent.FirstOrDefault()?.Artist.Id == p2.SongsLinkingToThisEvent.FirstOrDefault()?.Artist.Id)
            {
                continue;
            }

            if ((p2.DatePlayed - p1.DatePlayed) <= maxTimeBetween)
            {
                var key = p1.SongId.Value.ToString().CompareTo(p2.SongId.Value.ToString()) < 0
                    ? (p1.SongId.Value, p2.SongId.Value)
                    : (p2.SongId.Value, p1.SongId.Value);

                if (!pairings.ContainsKey(key))
                    pairings[key] = 0;
                pairings[key]++;
            }
        }

        return pairings.Where(kvp => kvp.Value > 2)
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => (
                _realm.Find<SongModel>(kvp.Key.Item1),
                _realm.Find<SongModel>(kvp.Key.Item2),
                kvp.Value
            ))
            .Where(t => t.Item1 != null && t.Item2 != null)
            .ToList();
    }

    /// <summary>
    /// Q: "What's the 'center of gravity' of my music library in terms of release year?"
    /// This is the play-count-weighted average release year.
    /// </summary>
    public double GetCenterOfGravityYear()
    {

        var yearPlays = _realm.All<DimmerPlayEvent>().ToList()
            .Select(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.ReleaseYear)
            .Where(y => y.HasValue)
            .Select(y => y.Value);

        return yearPlays.Any() ? yearPlays.Average() : 0;
    }

    /// <summary>
    /// Q: "Show me the 'Introvert vs. Extrovert' score of my music."
    /// Introvert: Long, complex, ambient, instrumental songs. Extrovert: Short, pop, party-tagged songs.
    /// </summary>
    public (double IntrovertScore, double ExtrovertScore) GetIntrovertExtrovertScore()
    {

        var allPlayedSongs = _realm.All<DimmerPlayEvent>().ToList()
            .Select(p => p.SongsLinkingToThisEvent.FirstOrDefault())
            .Where(s => s != null)
            .ToList();

        if (allPlayedSongs.Count==0)
            return (0, 0);

        var introvertScore = allPlayedSongs.Count(s => s.DurationInSeconds > 300 || !s.HasLyrics || s.Tags.Any(t => t.Name == "Ambient"));
        var extrovertScore = allPlayedSongs.Count(s => s.DurationInSeconds < 180 || s.Tags.Any(t => t.Name == "Party" || t.Name == "Pop"));

        return ((double)introvertScore / allPlayedSongs.Count, (double)extrovertScore / allPlayedSongs.Count);
    }

    // (9-21) More artistic & fun methods
    public GoldenRatioTrack? GetGoldenRatioSongInPlaylist(ObjectId playlistId) { /* Same logic as album version, but for a specific playlist */ return null; }
    public List<SongModel> GetSongsThatBreakTheMoldForAnArtist(ObjectId artistId) { /* Find songs by an artist whose genre is different from the artist's most common genre */ return new(); }
    public (double Morning, double Afternoon, double Evening, double Night) GetEnergyLevelByTimeOfDay() { /* Calculate the average "energy" (based on tags) of songs played during different time slots */ return (0, 0, 0, 0); }
    public List<SongModel> FindMyPaletteCleansers() { /* Songs I play after a long binge of a single artist or album */ return new(); }
    public (string? Theme, List<SongModel> Songs) FindHiddenConceptAlbum() { /* Find a standard studio album where I primarily listen to songs with a recurring lyrical theme (e.g., 'love', 'night', 'road') */ return (null, new()); }
    public double GetMusicalAnticipationScore() { /* (Plays of tracks 1-3 of unreleased albums I've pre-added) / (Total plays) */ return 0.0; }
    public double GetNostalgiaScore() { /* Percentage of listening time spent on songs released > 10 years ago */ return 0.0; }
    public List<(int Year, double AvgRating)> GetMyCriticalRatingCurve() { /* Plot the average rating I give to songs from each release year */ return new(); }
    public SongModel? GetMyPersonalAnthem() { /* The song with the highest combination of play count, rating, and low skip rate */ return null; }
    public bool DoesMyListeningFollowBenfordsLaw() { /* Check if the first digit of track numbers I play follows Benford's Law distribution. Purely for fun. */ return false; }
    public string GetMyMusicalColor() { /* Conceptual: Associate genres/moods with colors (e.g., Blues=Blue, Metal=Black, Pop=Yellow) and find the dominant color in listening history */ return "Gray"; }
    public List<SongModel> FindPalindromeSongs() { /* Songs whose title is a palindrome. e.g., "Aba" */ return [.. _realm.All<SongModel>().ToList().Where(s => s.Title.ToLower().Replace(" ", "") == new string([.. s.Title.ToLower().Replace(" ", "").Reverse()]))]; }
    public (double Predictability, SongModel? NextSong) GetNextSongPredictability() { /* Analyze history to see how often I play the same song after a specific other song. */ return (0.0, null); }

    #endregion
}