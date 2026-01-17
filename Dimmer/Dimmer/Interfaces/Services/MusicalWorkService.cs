using System.Text.RegularExpressions;

namespace Dimmer.Interfaces.Services;

/// <summary>
/// Implementation of Musical Work management service.
/// Handles linking songs to canonical works, filtering renditions, and suggesting matches.
/// </summary>
public partial class MusicalWorkService : IMusicalWorkService
{
    private readonly IRepository<MusicalWorkModel> _workRepo;
    private readonly IRepository<SongModel> _songRepo;

    public MusicalWorkService(
        IRepository<MusicalWorkModel> workRepo,
        IRepository<SongModel> songRepo)
    {
        _workRepo = workRepo ?? throw new ArgumentNullException(nameof(workRepo));
        _songRepo = songRepo ?? throw new ArgumentNullException(nameof(songRepo));
    }

    public MusicalWorkModel CreateWork(string title, string? composer = null, string? canonicalArtist = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        var work = new MusicalWorkModel
        {
            Id = ObjectId.GenerateNewId(),
            Title = title,
            Composer = composer,
            CanonicalArtist = canonicalArtist,
            DateCreated = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
            RenditionCount = 0
        };

        return _workRepo.Create(work);
    }

    public bool LinkSongToWork(ObjectId songId, ObjectId workId)
    {
        var success = _songRepo.Update(songId, song =>
        {
            // Get the work from repository to establish the relationship
            var work = _workRepo.GetById(workId);
            if (work != null)
            {
                song.MusicalWork = work;
            }
        });

        if (success)
        {
            // Update work statistics
            UpdateWorkStatistics(workId);
        }

        return success;
    }

    public bool UnlinkSongFromWork(ObjectId songId)
    {
        ObjectId? previousWorkId = null;

        var success = _songRepo.Update(songId, song =>
        {
            previousWorkId = song.MusicalWork?.Id;
            song.MusicalWork = null;
        });

        if (success && previousWorkId.HasValue)
        {
            // Update the work's statistics after unlinking
            UpdateWorkStatistics(previousWorkId.Value);
        }

        return success;
    }

    public List<SongModel> GetRenditions(ObjectId workId)
    {
        var work = _workRepo.GetById(workId);
        if (work == null)
        {
            return new List<SongModel>();
        }

        // Query songs that link to this work
        return _songRepo.Query(s => s.MusicalWork != null && s.MusicalWork.Id == workId);
    }

    public List<SongModel> GetFilteredRenditions(ObjectId workId, bool? instrumentalOnly = null, 
        bool? liveOnly = null, string? instrumentation = null)
    {
        var renditions = GetRenditions(workId);

        // Apply filters
        if (instrumentalOnly.HasValue)
        {
            renditions = renditions.Where(s => s.IsInstrumental == instrumentalOnly.Value).ToList();
        }

        if (liveOnly.HasValue)
        {
            renditions = renditions.Where(s => s.IsLivePerformance == liveOnly.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(instrumentation))
        {
            renditions = renditions.Where(s => 
                s.Instrumentation != null && 
                s.Instrumentation.Contains(instrumentation, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return renditions;
    }

    public List<WorkSuggestion> SuggestMatchingWorks(ObjectId songId, int maxResults = 5)
    {
        var song = _songRepo.GetById(songId);
        if (song == null)
        {
            return new List<WorkSuggestion>();
        }

        // If song is already linked, don't suggest
        if (song.MusicalWork != null)
        {
            return new List<WorkSuggestion>();
        }

        var allWorks = _workRepo.GetAll().ToList();
        var suggestions = new List<WorkSuggestion>();

        var normalizedSongTitle = NormalizeTitle(song.Title);

        foreach (var work in allWorks)
        {
            var score = CalculateSimilarityScore(song, work, normalizedSongTitle);
            if (score > 0.3) // Minimum confidence threshold
            {
                suggestions.Add(new WorkSuggestion
                {
                    Work = work,
                    ConfidenceScore = score,
                    Reason = BuildReasonString(song, work, score)
                });
            }
        }

        return suggestions
            .OrderByDescending(s => s.ConfidenceScore)
            .Take(maxResults)
            .ToList();
    }

    public List<SongSuggestion> SuggestMatchingSongs(ObjectId workId, int maxResults = 10)
    {
        var work = _workRepo.GetById(workId);
        if (work == null)
        {
            return new List<SongSuggestion>();
        }

        // Get all songs that are NOT already linked to any work
        var unlinkedSongs = _songRepo.Query(s => s.MusicalWork == null);
        var suggestions = new List<SongSuggestion>();

        var normalizedWorkTitle = NormalizeTitle(work.Title);

        foreach (var song in unlinkedSongs)
        {
            var score = CalculateSimilarityScore(song, work, normalizedWorkTitle);
            if (score > 0.3) // Minimum confidence threshold
            {
                suggestions.Add(new SongSuggestion
                {
                    Song = song,
                    ConfidenceScore = score,
                    Reason = BuildReasonString(song, work, score)
                });
            }
        }

        return suggestions
            .OrderByDescending(s => s.ConfidenceScore)
            .Take(maxResults)
            .ToList();
    }

    public MusicalWorkModel? GetWork(ObjectId workId)
    {
        return _workRepo.GetById(workId);
    }

    public List<MusicalWorkModel> GetAllWorks()
    {
        return _workRepo.GetAll().ToList();
    }

    public void UpdateWorkStatistics(ObjectId workId)
    {
        var renditions = GetRenditions(workId);

        _workRepo.Update(workId, work =>
        {
            work.RenditionCount = renditions.Count;
            work.TotalPlayCount = renditions.Sum(r => r.PlayCount);
            work.TotalPlayCompletedCount = renditions.Sum(r => r.PlayCompletedCount);
            
            var mostRecentPlay = renditions
                .Where(r => r.LastPlayed > DateTimeOffset.MinValue)
                .Select(r => r.LastPlayed)
                .OrderByDescending(d => d)
                .FirstOrDefault();
            
            if (mostRecentPlay > DateTimeOffset.MinValue)
            {
                work.LastPlayed = mostRecentPlay;
            }

            work.PopularityScore = renditions.Sum(r => r.PopularityScore) / Math.Max(1, renditions.Count);
            work.LastModified = DateTimeOffset.UtcNow;
        });
    }

    public bool UpdateRenditionMetadata(ObjectId songId, string? renditionType = null, 
        string? instrumentation = null, bool? isLive = null, bool? isRemix = null, 
        bool? isCover = null, string? notes = null)
    {
        return _songRepo.Update(songId, song =>
        {
            if (renditionType != null)
                song.RenditionType = renditionType;
            
            if (instrumentation != null)
                song.Instrumentation = instrumentation;
            
            if (isLive.HasValue)
                song.IsLivePerformance = isLive.Value;
            
            if (isRemix.HasValue)
                song.IsRemix = isRemix.Value;
            
            if (isCover.HasValue)
                song.IsCover = isCover.Value;
            
            if (notes != null)
                song.RenditionNotes = notes;
        });
    }

    public bool DeleteWork(ObjectId workId)
    {
        // First, unlink all renditions
        var renditions = GetRenditions(workId);
        foreach (var rendition in renditions)
        {
            _songRepo.Update(rendition.Id, song => song.MusicalWork = null);
        }

        // Then delete the work
        var work = _workRepo.GetById(workId);
        if (work != null)
        {
            _workRepo.Delete(work);
            return true;
        }

        return false;
    }

    public List<MusicalWorkModel> SearchWorksByTitle(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<MusicalWorkModel>();
        }

        var normalized = searchTerm.ToLowerInvariant().Trim();
        return _workRepo.Query(w => 
            w.Title.ToLower().Contains(normalized) ||
            (w.CanonicalArtist != null && w.CanonicalArtist.ToLower().Contains(normalized))
        );
    }

    #region Private Helper Methods

    /// <summary>
    /// Normalizes a title by removing common variation keywords and extra whitespace
    /// </summary>
    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var normalized = title.ToLowerInvariant();

        // Remove common variation keywords
        var keywords = new[]
        {
            "instrumental", "inst", "piano", "cello", "acoustic", "unplugged",
            "live", "remix", "remaster", "remastered", "cover", "version",
            "radio edit", "extended", "demo", "acoustic version", "live version",
            "album version", "single version", "feat", "featuring", "ft"
        };

        foreach (var keyword in keywords)
        {
            normalized = Regex.Replace(normalized, $@"\b{keyword}\b", "", RegexOptions.IgnoreCase);
        }

        // Remove parentheses and brackets content
        normalized = Regex.Replace(normalized, @"\([^)]*\)", "");
        normalized = Regex.Replace(normalized, @"\[[^\]]*\]", "");

        // Remove extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    /// <summary>
    /// Calculates a similarity score between a song and a work
    /// </summary>
    private double CalculateSimilarityScore(SongModel song, MusicalWorkModel work, string? preNormalizedTitle = null)
    {
        double score = 0.0;

        // Title similarity (40% weight)
        var normalizedSongTitle = preNormalizedTitle ?? NormalizeTitle(song.Title);
        var normalizedWorkTitle = NormalizeTitle(work.Title);
        var titleSimilarity = CalculateStringSimilarity(normalizedSongTitle, normalizedWorkTitle);
        score += titleSimilarity * 0.4;

        // Artist match (30% weight)
        if (!string.IsNullOrWhiteSpace(work.CanonicalArtist) && !string.IsNullOrWhiteSpace(song.ArtistName))
        {
            var artistSimilarity = CalculateStringSimilarity(
                song.ArtistName.ToLowerInvariant(), 
                work.CanonicalArtist.ToLowerInvariant()
            );
            score += artistSimilarity * 0.3;
        }

        // Composer match (20% weight)
        if (!string.IsNullOrWhiteSpace(work.Composer) && !string.IsNullOrWhiteSpace(song.Composer))
        {
            var composerSimilarity = CalculateStringSimilarity(
                song.Composer.ToLowerInvariant(),
                work.Composer.ToLowerInvariant()
            );
            score += composerSimilarity * 0.2;
        }

        // Genre match (10% weight)
        if (!string.IsNullOrWhiteSpace(work.Genre) && !string.IsNullOrWhiteSpace(song.GenreName))
        {
            if (song.GenreName.Equals(work.Genre, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.1;
            }
        }

        return Math.Min(score, 1.0); // Cap at 1.0
    }

    /// <summary>
    /// Calculates similarity between two strings using Levenshtein distance
    /// </summary>
    private static double CalculateStringSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            return 1.0;
        
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        // Exact match
        if (s1 == s2)
            return 1.0;

        // Contains check
        if (s1.Contains(s2) || s2.Contains(s1))
            return 0.8;

        // Levenshtein distance
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        
        if (maxLength == 0)
            return 1.0;

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var matrix = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= len2; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                var cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[len1, len2];
    }

    /// <summary>
    /// Builds a human-readable reason string for a match
    /// </summary>
    private static string BuildReasonString(SongModel song, MusicalWorkModel work, double score)
    {
        var reasons = new List<string>();

        var normalizedSongTitle = NormalizeTitle(song.Title);
        var normalizedWorkTitle = NormalizeTitle(work.Title);
        
        if (CalculateStringSimilarity(normalizedSongTitle, normalizedWorkTitle) > 0.8)
        {
            reasons.Add("Similar title");
        }

        if (!string.IsNullOrWhiteSpace(work.CanonicalArtist) && 
            !string.IsNullOrWhiteSpace(song.ArtistName) &&
            song.ArtistName.Equals(work.CanonicalArtist, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Same artist");
        }

        if (!string.IsNullOrWhiteSpace(work.Composer) && 
            !string.IsNullOrWhiteSpace(song.Composer) &&
            song.Composer.Equals(work.Composer, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Same composer");
        }

        if (reasons.Count == 0)
        {
            reasons.Add($"Match confidence: {score:P0}");
        }

        return string.Join(", ", reasons);
    }

    #endregion
}
