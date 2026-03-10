using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel.SingleSongVMSection;


public partial class EditSongViewModel : ObservableObject
{
    private readonly BaseViewModelWin _mainViewModel;
    private readonly IRealmFactory _realmFactory;

    [ObservableProperty]
    public partial SongModelView OriginalSong { get; set; }

    [ObservableProperty]
    public partial SongModelView EditingSong { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<PropertyChangeModelView> PendingChanges { get; set; } = new();

    [ObservableProperty]
    public partial  bool HasChanges { get; set; }

    [ObservableProperty]
    public partial  int TotalChangesCount { get; set; }

    [ObservableProperty]
    public partial  int AcceptedChangesCount { get; set; }

    [ObservableProperty]
    public partial  bool IsReviewPopupOpen { get; set; }

    // For artist selection
    [ObservableProperty]
    public partial  List<string> AllArtists { get; set; }

    [ObservableProperty]
    public partial  ObservableCollection<string?> SelectedArtists { get; set; } = new();

    // Track original artists for comparison
    List<string?> _originalArtistNames;

    // Change tracking dictionary for quick lookup
    private Dictionary<string, PropertyChangeModelView> _changeMap = new();

    public EditSongViewModel(BaseViewModelWin mainViewModel, SongModelView songToEdit)
    {
        _mainViewModel = mainViewModel;
        _realmFactory = mainViewModel.RealmFactory;

        OriginalSong = songToEdit;
        EditingSong = songToEdit.ShallowCopy();

        // Store original artists
        _originalArtistNames = OriginalSong.ArtistToSong?
            .Select(a => a.Name)
            .ToList() ?? new List<string?>();

        SelectedArtists = new ObservableCollection<string?>(_originalArtistNames);

        LoadAllArtists();

        // Subscribe to property changes
        EditingSong.PropertyChanged += OnEditingSongPropertyChanged;
    }

    private void LoadAllArtists()
    {
        AllArtists = _realmFactory.GetRealmInstance()
            .All<ArtistModel>().AsEnumerable()
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => a.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();
    }

    private void OnEditingSongPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SongModelView.ArtistToSong))
            return; // Handle artists separately

        var oldValue = GetOriginalValue(e.PropertyName);
        var newValue = GetCurrentValue(e.PropertyName);

        if (!AreValuesEqual(oldValue, newValue))
        {
            AddOrUpdateChange(e.PropertyName, oldValue, newValue);
        }
        else
        {
            RemoveChangeIfExists(e.PropertyName);
        }

        UpdateHasChanges();
    }

    private object? GetOriginalValue(string? propertyName)
    {
        if (propertyName == null) return null;
        return propertyName switch
        {
            nameof(SongModelView.Title) => OriginalSong.Title,
            nameof(SongModelView.TrackNumber) => OriginalSong.TrackNumber,
            nameof(SongModelView.ReleaseYear) => OriginalSong.ReleaseYear,
            nameof(SongModelView.Conductor) => OriginalSong.Conductor,
            nameof(SongModelView.Composer) => OriginalSong.Composer,
            nameof(SongModelView.Description) => OriginalSong.Description,
            nameof(SongModelView.IsInstrumental) => OriginalSong.IsInstrumental,
            nameof(SongModelView.GenreName) => OriginalSong.GenreName,
            nameof(SongModelView.AlbumName) => OriginalSong.AlbumName,
            nameof(SongModelView.CoverImagePath) => OriginalSong.CoverImagePath,
            nameof(SongModelView.Lyricist) => OriginalSong.Lyricist,
            nameof(SongModelView.BPM) => OriginalSong.BPM,
            nameof(SongModelView.Language) => OriginalSong.Language,
            nameof(SongModelView.DiscNumber) => OriginalSong.DiscNumber,
            nameof(SongModelView.DiscTotal) => OriginalSong.DiscTotal,
            _ => null
        };
    }

    private object? GetCurrentValue(string? propertyName)
    {
        if (propertyName == null) return null;
        return propertyName switch
        {
            nameof(SongModelView.Title) => EditingSong.Title,
            nameof(SongModelView.TrackNumber) => EditingSong.TrackNumber,
            nameof(SongModelView.ReleaseYear) => EditingSong.ReleaseYear,
            nameof(SongModelView.Conductor) => EditingSong.Conductor,
            nameof(SongModelView.Composer) => EditingSong.Composer,
            nameof(SongModelView.Description) => EditingSong.Description,
            nameof(SongModelView.IsInstrumental) => EditingSong.IsInstrumental,
            nameof(SongModelView.GenreName) => EditingSong.GenreName,
            nameof(SongModelView.AlbumName) => EditingSong.AlbumName,
            nameof(SongModelView.CoverImagePath) => EditingSong.CoverImagePath,
            nameof(SongModelView.Lyricist) => EditingSong.Lyricist,
            nameof(SongModelView.BPM) => EditingSong.BPM,
            nameof(SongModelView.Language) => EditingSong.Language,
            nameof(SongModelView.DiscNumber) => EditingSong.DiscNumber,
            nameof(SongModelView.DiscTotal) => EditingSong.DiscTotal,
            _ => null
        };
    }

    private bool AreValuesEqual(object? oldVal, object? newVal)
    {
        if (oldVal == null && newVal == null) return true;
        if (oldVal == null || newVal == null) return false;

        // Handle special cases
        if (oldVal is double d1 && newVal is double d2)
            return Math.Abs(d1 - d2) < 0.001;

        if (oldVal is float f1 && newVal is float f2)
            return Math.Abs(f1 - f2) < 0.001f;

        return oldVal.Equals(newVal);
    }

    private bool ShouldSkipTracking(string propertyName)
    {
        // Skip auto-calculated properties
        return propertyName switch
        {
            nameof(SongModelView.DurationFormatted) => true,
            nameof(SongModelView.SearchableText) => true,
            nameof(SongModelView.UserNoteAggregatedText) => true,
            nameof(SongModelView.TitleDurationKey) => true,
            nameof(SongModelView.CurrentPlaySongDominantColor) => true,
            _ => false
        };
    }
    private void AddOrUpdateChange(string? propertyName, object? oldValue, object? newValue)
    {
        if(propertyName is null) return;
        if (ShouldSkipTracking(propertyName))
            return;

        var displayName = GetDisplayName(propertyName);
        var category = GetCategory(propertyName);
        var order = GetDisplayOrder(propertyName);

        // Handle null/empty string normalization
        if (oldValue is string oldStr && string.IsNullOrEmpty(oldStr))
            oldValue = null;
        if (newValue is string newStr && string.IsNullOrEmpty(newStr))
            newValue = null;

        if (_changeMap.TryGetValue(propertyName, out var existing))
        {
            // Update existing change
            existing.NewValue = newValue;
            existing.IsAccepted = false;
            existing.IsRejected = false;
        }
        else
        {
            // Create new change
            var change = new PropertyChangeModelView() // Pass ViewModel reference
            {
                PropertyName = propertyName,
                DisplayName = displayName,
                OldValue = oldValue,
                NewValue = newValue,
                Category = category,
                DisplayOrder = order
            };

            _changeMap[propertyName] = change;

            // Insert in order
            var insertIndex = PendingChanges
                .TakeWhile(c => c.DisplayOrder <= order)
                .Count();

            // Ensure we insert at the right position
            while (insertIndex < PendingChanges.Count &&
                   PendingChanges[insertIndex].DisplayOrder == order &&
                   string.Compare(PendingChanges[insertIndex].DisplayName, displayName) < 0)
            {
                insertIndex++;
            }

            PendingChanges.Insert(insertIndex, change);
        }

        TotalChangesCount = PendingChanges.Count;
    }

    private void RemoveChangeIfExists(string? propertyName)
    {
        if (propertyName is null) return;
        if (_changeMap.TryGetValue(propertyName, out var change))
        {
            PendingChanges.Remove(change);
            _changeMap.Remove(propertyName);
        }

        TotalChangesCount = PendingChanges.Count;
    }

    private string GetDisplayName(string propertyName)
    {
        return propertyName switch
        {
            nameof(SongModelView.Title) => "Title",
            nameof(SongModelView.TrackNumber) => "Track #",
            nameof(SongModelView.ReleaseYear) => "Release Year",
            nameof(SongModelView.Conductor) => "Conductor",
            nameof(SongModelView.Composer) => "Composer",
            nameof(SongModelView.Description) => "Description",
            nameof(SongModelView.IsInstrumental) => "Instrumental",
            nameof(SongModelView.GenreName) => "Genre",
            nameof(SongModelView.AlbumName) => "Album",
            nameof(SongModelView.CoverImagePath) => "Cover Art",
            nameof(SongModelView.Lyricist) => "Lyricist",
            nameof(SongModelView.BPM) => "BPM",
            nameof(SongModelView.Language) => "Language",
            nameof(SongModelView.DiscNumber) => "Disc #",
            nameof(SongModelView.DiscTotal) => "Total Discs",
            _ => propertyName
        };
    }

    private string GetCategory(string propertyName)
    {
        return propertyName switch
        {
            nameof(SongModelView.Title) or
            nameof(SongModelView.TrackNumber) or
            nameof(SongModelView.ReleaseYear) or
            nameof(SongModelView.IsInstrumental) => "Basic Info",

            nameof(SongModelView.Conductor) or
            nameof(SongModelView.Composer) or
            nameof(SongModelView.Lyricist) or
            nameof(SongModelView.BPM) => "Credits",

            nameof(SongModelView.AlbumName) or
            nameof(SongModelView.CoverImagePath) or
            nameof(SongModelView.DiscNumber) or
            nameof(SongModelView.DiscTotal) => "Album",

            nameof(SongModelView.GenreName) => "Genre",
            nameof(SongModelView.Language) => "Language",
            nameof(SongModelView.Description) => "Description",

            _ => "Other"
        };
    }

    private int GetDisplayOrder(string propertyName)
    {
        return propertyName switch
        {
            nameof(SongModelView.Title) => 1,
            nameof(SongModelView.TrackNumber) => 2,
            nameof(SongModelView.ReleaseYear) => 3,
            nameof(SongModelView.IsInstrumental) => 4,
            nameof(SongModelView.GenreName) => 5,
            nameof(SongModelView.AlbumName) => 6,
            nameof(SongModelView.CoverImagePath) => 7,
            nameof(SongModelView.Composer) => 8,
            nameof(SongModelView.Conductor) => 9,
            nameof(SongModelView.Lyricist) => 10,
            nameof(SongModelView.BPM) => 11,
            nameof(SongModelView.Language) => 12,
            nameof(SongModelView.DiscNumber) => 13,
            nameof(SongModelView.DiscTotal) => 14,
            nameof(SongModelView.Description) => 15,
            _ => 100
        };
    }

    // Artist change tracking
    public void UpdateSelectedArtists(IEnumerable<string> artists)
    {
        SelectedArtists = new ObservableCollection<string>(artists.Distinct());
        CheckArtistChanges();
    }

    public void AddArtist(string artistName)
    {
        if (!SelectedArtists.Contains(artistName))
        {
            SelectedArtists.Add(artistName);
            CheckArtistChanges();
        }
    }

    public void RemoveArtist(string artistName)
    {
        SelectedArtists.Remove(artistName);
        CheckArtistChanges();
    }

    private void CheckArtistChanges()
    {
        var currentArtists = SelectedArtists.OrderBy(n => n).ToList();
        var originalArtists = _originalArtistNames.OrderBy(n => n).ToList();

        if (!currentArtists.SequenceEqual(originalArtists))
        {
            var oldValue = string.Join(", ", originalArtists);
            var newValue = string.Join(", ", currentArtists);

            if (string.IsNullOrEmpty(oldValue)) oldValue = "<none>";
            if (string.IsNullOrEmpty(newValue)) newValue = "<none>";

            AddOrUpdateChange("Artists", oldValue, newValue);
        }
        else
        {
            RemoveChangeIfExists("Artists");
        }

        UpdateHasChanges();
    }

    private void UpdateHasChanges()
    {
        HasChanges = PendingChanges.Any();
        AcceptedChangesCount = PendingChanges.Count(c => c.IsAccepted);
    }

    // Change acceptance/rejection
    public void AcceptChange(PropertyChangeModelView change)
    {
        change.IsAccepted = true;
        change.IsRejected = false;

        // Apply to editing song
        ApplyChangeToEditingSong(change);
        UpdateHasChanges();
    }

    public void RejectChange(PropertyChangeModelView change)
    {
        change.IsRejected = true;
        change.IsAccepted = false;

        // Revert in editing song
        RevertChangeInEditingSong(change);
        UpdateHasChanges();
    }

    public void AcceptAllChanges()
    {
        foreach (var change in PendingChanges.ToList())
        {
            change.IsAccepted = true;
            change.WasAutoAccepted = true;
            ApplyChangeToEditingSong(change);
        }
        UpdateHasChanges();
    }

    public void RejectAllChanges()
    {
        foreach (var change in PendingChanges.ToList())
        {
            change.IsRejected = true;
            RevertChangeInEditingSong(change);
        }

        // Clear all changes
        PendingChanges.Clear();
        _changeMap.Clear();
        UpdateHasChanges();
    }

    private void ApplyChangeToEditingSong(PropertyChangeModelView change)
    {
        // When accepting a change, we keep the current value in EditingSong
        // But we mark it as accepted so it will be saved

        // For complex properties that need special handling
        switch (change.PropertyName)
        {
            case "Artists":
                // Artists are already in SelectedArtists, just mark as accepted
                break;

            case nameof(SongModelView.Genre):
            case nameof(SongModelView.Album):
            case nameof(SongModelView.Artist):
                // Navigation properties - handled by their Name properties
                break;

            default:
                // For simple properties, the value is already in EditingSong
                // No action needed, just marking as accepted
                break;
        }
    }

    private void RevertChangeInEditingSong(PropertyChangeModelView change)
    {
        // Revert the value in EditingSong back to original
        switch (change.PropertyName)
        {
            // Basic Info
            case nameof(SongModelView.Title):
                EditingSong.Title = OriginalSong.Title;
                break;

            case nameof(SongModelView.TrackNumber):
                EditingSong.TrackNumber = OriginalSong.TrackNumber;
                break;

            case nameof(SongModelView.ReleaseYear):
                EditingSong.ReleaseYear = OriginalSong.ReleaseYear;
                break;

            case nameof(SongModelView.IsInstrumental):
                EditingSong.IsInstrumental = OriginalSong.IsInstrumental;
                break;

            case nameof(SongModelView.DurationInSeconds):
                EditingSong.DurationInSeconds = OriginalSong.DurationInSeconds;
                // Also update derived properties
                EditingSong.SetTitleAndDuration(EditingSong.Title, EditingSong.DurationInSeconds);
                break;

            // Credits & Metadata
            case nameof(SongModelView.Composer):
                EditingSong.Composer = OriginalSong.Composer;
                break;

            case nameof(SongModelView.Conductor):
                EditingSong.Conductor = OriginalSong.Conductor;
                break;

            case nameof(SongModelView.Lyricist):
                EditingSong.Lyricist = OriginalSong.Lyricist;
                break;

            case nameof(SongModelView.BPM):
                EditingSong.BPM = OriginalSong.BPM;
                break;

            case nameof(SongModelView.Language):
                EditingSong.Language = OriginalSong.Language;
                break;

            case nameof(SongModelView.Description):
                EditingSong.Description = OriginalSong.Description;
                break;

            // Album related
            case nameof(SongModelView.AlbumName):
                EditingSong.AlbumName = OriginalSong.AlbumName;
                break;

            case nameof(SongModelView.CoverImagePath):
                EditingSong.CoverImagePath = OriginalSong.CoverImagePath;
                break;

            case nameof(SongModelView.DiscNumber):
                EditingSong.DiscNumber = OriginalSong.DiscNumber;
                break;

            case nameof(SongModelView.DiscTotal):
                EditingSong.DiscTotal = OriginalSong.DiscTotal;
                break;

            case nameof(SongModelView.TrackTotal):
                EditingSong.TrackTotal = OriginalSong.TrackTotal;
                break;

            // Genre
            case nameof(SongModelView.GenreName):
                EditingSong.GenreName = OriginalSong.GenreName;
                // Also reset the Genre navigation property if needed
                if (EditingSong.Genre != null && OriginalSong.Genre != null)
                {
                    EditingSong.Genre.Name = OriginalSong.Genre.Name;
                }
                break;

            // Artists (special handling)
            case "Artists":
                // Reset the artists collection
                SelectedArtists = new ObservableCollection<string?>(_originalArtistNames);

                // Also reset the ArtistToSong collection in EditingSong if needed
                if (OriginalSong.ArtistToSong != null)
                {
                    EditingSong.ArtistToSong = new ObservableCollection<ArtistModelView?>();
                    foreach (var artist in OriginalSong.ArtistToSong)
                    {
                        if (artist != null)
                        {
                            EditingSong.ArtistToSong.Add(new ArtistModelView
                            {
                                Id = artist.Id,
                                Name = artist.Name,
                                TotalSongsByArtist = artist.TotalSongsByArtist
                            });
                        }
                    }
                }
                break;

            // File & Technical Info
            case nameof(SongModelView.FilePath):
                EditingSong.FilePath = OriginalSong.FilePath;
                break;

            case nameof(SongModelView.FileFormat):
                EditingSong.FileFormat = OriginalSong.FileFormat;
                break;

            case nameof(SongModelView.FileSize):
                EditingSong.FileSize = OriginalSong.FileSize;
                break;

            case nameof(SongModelView.BitRate):
                EditingSong.BitRate = OriginalSong.BitRate;
                break;

            case nameof(SongModelView.SampleRate):
                EditingSong.SampleRate = OriginalSong.SampleRate;
                break;

            case nameof(SongModelView.BitDepth):
                EditingSong.BitDepth = OriginalSong.BitDepth;
                break;

            case nameof(SongModelView.NbOfChannels):
                EditingSong.NbOfChannels = OriginalSong.NbOfChannels;
                break;

            case nameof(SongModelView.Encoder):
                EditingSong.Encoder = OriginalSong.Encoder;
                break;

            // Lyrics
            case nameof(SongModelView.HasLyrics):
                EditingSong.HasLyrics = OriginalSong.HasLyrics;
                break;

            case nameof(SongModelView.HasSyncedLyrics):
                EditingSong.HasSyncedLyrics = OriginalSong.HasSyncedLyrics;
                break;

            case nameof(SongModelView.SyncLyrics):
                EditingSong.SyncLyrics = OriginalSong.SyncLyrics;
                break;

            case nameof(SongModelView.UnSyncLyrics):
                EditingSong.UnSyncLyrics = OriginalSong.UnSyncLyrics;
                break;

            // Play stats (should probably not be editable, but just in case)
            case nameof(SongModelView.Rating):
                EditingSong.Rating = OriginalSong.Rating;
                break;

            case nameof(SongModelView.IsFavorite):
                EditingSong.IsFavorite = OriginalSong.IsFavorite;
                break;

            case nameof(SongModelView.PlayCount):
                EditingSong.PlayCount = OriginalSong.PlayCount;
                break;

            case nameof(SongModelView.PlayCompletedCount):
                EditingSong.PlayCompletedCount = OriginalSong.PlayCompletedCount;
                break;

            case nameof(SongModelView.SkipCount):
                EditingSong.SkipCount = OriginalSong.SkipCount;
                break;

            case nameof(SongModelView.LastPlayed):
                EditingSong.LastPlayed = OriginalSong.LastPlayed;
                break;

            case nameof(SongModelView.PauseCount):
                EditingSong.PauseCount = OriginalSong.PauseCount;
                break;

            case nameof(SongModelView.ResumeCount):
                EditingSong.ResumeCount = OriginalSong.ResumeCount;
                break;

            case nameof(SongModelView.SeekCount):
                EditingSong.SeekCount = OriginalSong.SeekCount;
                break;

            case nameof(SongModelView.ListenThroughRate):
                EditingSong.ListenThroughRate = OriginalSong.ListenThroughRate;
                break;

            case nameof(SongModelView.SkipRate):
                EditingSong.SkipRate = OriginalSong.SkipRate;
                break;

            case nameof(SongModelView.EngagementScore):
                EditingSong.EngagementScore = OriginalSong.EngagementScore;
                break;

            case nameof(SongModelView.PopularityScore):
                EditingSong.PopularityScore = OriginalSong.PopularityScore;
                break;

            case nameof(SongModelView.GlobalRank):
                EditingSong.GlobalRank = OriginalSong.GlobalRank;
                break;

            case nameof(SongModelView.RankInAlbum):
                EditingSong.RankInAlbum = OriginalSong.RankInAlbum;
                break;

            case nameof(SongModelView.RankInArtist):
                EditingSong.RankInArtist = OriginalSong.RankInArtist;
                break;

            // Device Info
            case nameof(SongModelView.DeviceName):
                EditingSong.DeviceName = OriginalSong.DeviceName;
                break;

            case nameof(SongModelView.DeviceFormFactor):
                EditingSong.DeviceFormFactor = OriginalSong.DeviceFormFactor;
                break;

            case nameof(SongModelView.DeviceModel):
                EditingSong.DeviceModel = OriginalSong.DeviceModel;
                break;

            case nameof(SongModelView.DeviceManufacturer):
                EditingSong.DeviceManufacturer = OriginalSong.DeviceManufacturer;
                break;

            case nameof(SongModelView.DeviceVersion):
                EditingSong.DeviceVersion = OriginalSong.DeviceVersion;
                break;

            // Segment/Song Type
            case nameof(SongModelView.SongType):
                EditingSong.SongTypeValue = OriginalSong.SongTypeValue;
                break;

            case nameof(SongModelView.ParentSongId):
                EditingSong.ParentSongId = OriginalSong.ParentSongId;
                break;

            case nameof(SongModelView.SegmentStartTime):
                EditingSong.SegmentStartTime = OriginalSong.SegmentStartTime;
                break;

            case nameof(SongModelView.SegmentEndTime):
                EditingSong.SegmentEndTime = OriginalSong.SegmentEndTime;
                break;

            case nameof(SongModelView.SegmentEndBehavior):
                EditingSong.SegmentEndBehaviorValue = OriginalSong.SegmentEndBehaviorValue;
                break;

            // Misc
            case nameof(SongModelView.Achievement):
                EditingSong.Achievement = OriginalSong.Achievement;
                break;

            case nameof(SongModelView.IsHidden):
                EditingSong.IsHidden = OriginalSong.IsHidden;
                break;

            case nameof(SongModelView.CoverArtHash):
                EditingSong.CoverArtHash = OriginalSong.CoverArtHash;
                break;

            case nameof(SongModelView.DiscoveryDate):
                EditingSong.DiscoveryDate = OriginalSong.DiscoveryDate;
                break;

            case nameof(SongModelView.FirstPlayed):
                EditingSong.FirstPlayed = OriginalSong.FirstPlayed;
                break;

            case nameof(SongModelView.PlayStreakDays):
                EditingSong.PlayStreakDays = OriginalSong.PlayStreakDays;
                break;

            case nameof(SongModelView.EddingtonNumber):
                EditingSong.EddingtonNumber = OriginalSong.EddingtonNumber;
                break;

            // Auto-calculated fields that should be refreshed
            case nameof(SongModelView.SearchableText):
            case nameof(SongModelView.UserNoteAggregatedText):
            case nameof(SongModelView.DurationFormatted):
                // These are derived, so we don't revert them directly
                // They'll be recalculated when needed
                break;

            // Collections (special handling)
            case nameof(SongModelView.UserNoteAggregatedCol):
                if (OriginalSong.UserNoteAggregatedCol != null)
                {
                    EditingSong.UserNoteAggregatedCol = new ObservableCollection<UserNoteModelView>();
                    foreach (var note in OriginalSong.UserNoteAggregatedCol)
                    {
                        if (note != null)
                        {
                            EditingSong.UserNoteAggregatedCol.Add(new UserNoteModelView
                            {
                                Id = note.Id,
                                UserMessageText = note.UserMessageText,
                                CreatedAt = note.CreatedAt,
                                ModifiedAt = note.ModifiedAt,
                                UserMessageImagePath = note.UserMessageImagePath,
                                UserMessageAudioPath = note.UserMessageAudioPath,
                                IsPinned = note.IsPinned,
                                UserRating = note.UserRating,
                                MessageColor = note.MessageColor
                            });
                        }
                    }
                }
                break;

            case nameof(SongModelView.PlayEvents):
            case nameof(SongModelView.PlaylistsHavingSong):
            case nameof(SongModelView.EmbeddedSync):
                // These collections should typically not be editable in this view
                // But if they are, revert them here
                break;

            default:
                Debug.WriteLine($"Unhandled property revert: {change.PropertyName}");
                break;
        }

        // After reverting, refresh any dependent properties
        EditingSong.RefreshDenormalizedProperties();
    }
    // Save only accepted changes
    public async Task SaveAcceptedChangesAsync()
    {
        var acceptedChanges = PendingChanges
            .Where(c => c.IsAccepted)
            .ToList();

        if (!acceptedChanges.Any())
            return;

        // Apply changes to original song
        foreach (var change in acceptedChanges)
        {
            if (change.PropertyName == "Artists")
            {
                await UpdateArtistsAsync(SelectedArtists.ToList());
            }
            else
            {
                // Copy value from editing to original
                var value = GetCurrentValue(change.PropertyName);
                SetOriginalValue(change.PropertyName, value);
            }
        }

        // Save to database
        await _mainViewModel.ApplyNewSongEdits(OriginalSong);

        // Clear pending changes
        PendingChanges.Clear();
        _changeMap.Clear();
        UpdateHasChanges();

        // Update original artists list
        _originalArtistNames = SelectedArtists.ToList();
    }

    private void SetOriginalValue(string propertyName, object value)
    {
        switch (propertyName)
        {
            case nameof(SongModelView.Title):
                OriginalSong.Title = (string)value;
                break;
            case nameof(SongModelView.TrackNumber):
                OriginalSong.TrackNumber = (int?)value;
                break;
                // ... etc
        }
    }

    private async Task UpdateArtistsAsync(List<string>? artistNames)
    {
        throw new NotImplementedException();
        // Implementation depends on your artist management logic
        await Task.CompletedTask;
    }

    // Discard all changes
    public void DiscardAllChanges()
    {
        // Reset editing song to original
        EditingSong = OriginalSong.ShallowCopy();

        // Reset artists
        SelectedArtists = new ObservableCollection<string>(_originalArtistNames);

        // Clear changes
        PendingChanges.Clear();
        _changeMap.Clear();
        UpdateHasChanges();
    }
}