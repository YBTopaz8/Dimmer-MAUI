using CommunityToolkit.Maui.Core.Extensions;


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
    public partial bool HasChanges { get; set; }

    [ObservableProperty]
    public partial int TotalChangesCount { get; set; }

    [ObservableProperty]
    public partial int AcceptedChangesCount { get; set; }

    [ObservableProperty]
    public partial bool IsReviewPopupOpen { get; set; }

    // For artist selection
    [ObservableProperty]
    public partial List<string> AllArtists { get; set; }


    // Track original artists for comparison
    List<string> _originalArtistNames;

    // Change tracking dictionary for quick lookup
    private Dictionary<string, PropertyChangeModelView> _changeMap = new();
    // Artists Management

    [ObservableProperty]
    public partial ObservableCollection<string> ArtistSuggestions { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<string> AllArtistsMasterList { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<string> SelectedArtists { get; set; } = new();

    // Album Arts from Album
    [ObservableProperty]
    public partial ObservableCollection<string> AlbumArtCandidates { get; set; } = new();

    [ObservableProperty]
    public partial bool HasAlbumArtCandidates { get; set; }

    // Online Metadata (Last.fm)
    [ObservableProperty]
    public partial bool IsSearchingOnline { get; set; }

    [ObservableProperty]
    public partial bool HasOnlineResults { get; set; }

    [ObservableProperty]
    public partial string OnlineSearchTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OnlineSearchArtist { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OnlineSearchAlbum { get; set; } = string.Empty;

    private dynamic? _onlineMetadataResult;
    public dynamic? OnlineMetadataResult
    {
        get => _onlineMetadataResult;
        set
        {
            if (!ReferenceEquals(_onlineMetadataResult, value))
            {
                OnPropertyChanging(nameof(OnlineMetadataResult));
                _onlineMetadataResult = value;
                OnPropertyChanged(nameof(OnlineMetadataResult));
            }
        }
    }

    // Notifications
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    private static readonly Dictionary<string, (string Display, string Category, int Order)> TrackedProperties = new()
    {
        { "Artists", ("Artists Linked", "Credits", 0) },
        { nameof(SongModelView.Title), ("Title", "Basic Info", 1) },
        { nameof(SongModelView.TrackNumber), ("Track #", "Basic Info", 2) },
        { nameof(SongModelView.TrackTotal), ("Track Total", "Basic Info", 3) },
        { nameof(SongModelView.DiscNumber), ("Disc #", "Basic Info", 4) },
        { nameof(SongModelView.DiscTotal), ("Disc Total", "Basic Info", 5) },
        { nameof(SongModelView.ReleaseYear), ("Release Year", "Basic Info", 6) },
        { nameof(SongModelView.IsInstrumental), ("Instrumental", "Basic Info", 7) },
        { nameof(SongModelView.AlbumName), ("Album Name", "Album", 8) },
        { nameof(SongModelView.GenreName), ("Genre", "Genre", 9) },
        { nameof(SongModelView.CoverImagePath), ("Cover Image", "Artwork", 10) },
        { nameof(SongModelView.Composer), ("Composer", "Credits", 11) },
        { nameof(SongModelView.Conductor), ("Conductor", "Credits", 12) },
        { nameof(SongModelView.Lyricist), ("Lyricist", "Credits", 13) },
        { nameof(SongModelView.BPM), ("BPM", "Audio Specs", 14) },
        { nameof(SongModelView.Language), ("Language", "Metadata", 15) },
        { nameof(SongModelView.Description), ("Description", "Metadata", 16) },
        { nameof(SongModelView.SyncLyrics), ("Synced Lyrics", "Lyrics", 17) },
        { nameof(SongModelView.UnSyncLyrics), ("Unsynced Lyrics", "Lyrics", 18) },
        { nameof(SongModelView.Rating), ("Rating", "Statistics", 19) },
        { nameof(SongModelView.IsFavorite), ("Favorite Status", "Statistics", 20) },
        { nameof(SongModelView.Achievement), ("Achievement", "Metadata", 21) },
        { nameof(SongModelView.DurationInSeconds), ("Duration", "Audio Specs", 22) }
    };
    [ObservableProperty]
    public partial bool IsStatusOpen { get; set; }

    public EditSongViewModel(BaseViewModelWin mainViewModel, SongModelView songToEdit)
    {
        _mainViewModel = mainViewModel;
        _realmFactory = mainViewModel.RealmFactory;

        OriginalSong = songToEdit;
        EditingSong = songToEdit.ShallowCopy();

        _originalArtistNames = OriginalSong.ArtistToSong?
            .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => a!.Name)
            .ToList() ?? new List<string>();

        if (!_originalArtistNames.Any() && !string.IsNullOrWhiteSpace(OriginalSong.ArtistName))
        {
            _originalArtistNames.Add(OriginalSong.ArtistName);
        }

        SelectedArtists = new ObservableCollection<string>(_originalArtistNames);

        // Preload fields for online search
        OnlineSearchTitle = EditingSong.Title ?? string.Empty;
        OnlineSearchArtist = EditingSong.ArtistName ?? string.Empty;
        OnlineSearchAlbum = EditingSong.AlbumName ?? string.Empty;

        LoadArtistsMasterList();
        LoadAlbumArtCandidates();

        EditingSong.PropertyChanged += OnEditingSongPropertyChanged;
    }


    private void LoadArtistsMasterList()
    {
        try
        {
            var realm = _realmFactory.GetRealmInstance();
            AllArtistsMasterList = realm.All<ArtistModel>()
                .AsEnumerable()
                .Where(a => !string.IsNullOrWhiteSpace(a.Name))
                .Select(a => a.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToObservableCollection();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load artists master list: {ex.Message}");
        }
    }

    private void LoadAlbumArtCandidates()
    {
        try
        {
            var realm = _realmFactory.GetRealmInstance();
            var dbSong = realm.Find<SongModel>(OriginalSong.Id);
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (dbSong?.Album != null)
            {
                var paths = dbSong.Album.SongsInAlbum
                    .Select(s => s.CoverImagePath)
                    .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));

                foreach (var p in paths) candidates.Add(p);
            }
            else if (!string.IsNullOrWhiteSpace(OriginalSong.AlbumName))
            {
                var albumFromDb = realm.All<AlbumModel>().FirstOrDefault(a => a.Name == OriginalSong.AlbumName);
                if (albumFromDb?.SongsInAlbum != null)
                {
                    var paths = albumFromDb.SongsInAlbum
                        .Select(s => s.CoverImagePath)
                        .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));

                    foreach (var p in paths) candidates.Add(p);
                }
            }

            AlbumArtCandidates = new ObservableCollection<string>(candidates);
            HasAlbumArtCandidates = AlbumArtCandidates.Any();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error querying album art candidates: {ex.Message}");
        }
    }


    private void OnEditingSongPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || !TrackedProperties.ContainsKey(e.PropertyName))
            return;

        var prop = typeof(SongModelView).GetProperty(e.PropertyName);
        if (prop == null) return;

        var originalVal = prop.GetValue(OriginalSong);
        var currentVal = prop.GetValue(EditingSong);

        if (!AreValuesEqual(originalVal, currentVal))
        {
            var info = TrackedProperties[e.PropertyName];
            AddOrUpdateChange(e.PropertyName, info.Display, info.Category, info.Order, originalVal, currentVal);
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
            nameof(SongModelView.TrackTotal) => OriginalSong.TrackTotal,
            nameof(SongModelView.DiscNumber) => OriginalSong.DiscNumber,
            nameof(SongModelView.DiscTotal) => OriginalSong.DiscTotal,
            nameof(SongModelView.ReleaseYear) => OriginalSong.ReleaseYear,
            nameof(SongModelView.Conductor) => OriginalSong.Conductor,
            nameof(SongModelView.Composer) => OriginalSong.Composer,
            nameof(SongModelView.Lyricist) => OriginalSong.Lyricist,
            nameof(SongModelView.BPM) => OriginalSong.BPM,
            nameof(SongModelView.Language) => OriginalSong.Language,
            nameof(SongModelView.Description) => OriginalSong.Description,
            nameof(SongModelView.IsInstrumental) => OriginalSong.IsInstrumental,
            nameof(SongModelView.GenreName) => OriginalSong.GenreName,
            nameof(SongModelView.AlbumName) => OriginalSong.AlbumName,
            nameof(SongModelView.CoverImagePath) => OriginalSong.CoverImagePath,
            nameof(SongModelView.SyncLyrics) => OriginalSong.SyncLyrics,
            nameof(SongModelView.UnSyncLyrics) => OriginalSong.UnSyncLyrics,
            nameof(SongModelView.Rating) => OriginalSong.Rating,
            nameof(SongModelView.IsFavorite) => OriginalSong.IsFavorite,
            nameof(SongModelView.Achievement) => OriginalSong.Achievement,
            nameof(SongModelView.DurationInSeconds) => OriginalSong.DurationInSeconds,
            nameof(SongModelView.FilePath) => OriginalSong.FilePath,
            nameof(SongModelView.FileFormat) => OriginalSong.FileFormat,
            nameof(SongModelView.FileSize) => OriginalSong.FileSize,
            nameof(SongModelView.BitRate) => OriginalSong.BitRate,
            nameof(SongModelView.BitDepth) => OriginalSong.BitDepth,
            nameof(SongModelView.SampleRate) => OriginalSong.SampleRate,
            nameof(SongModelView.NbOfChannels) => OriginalSong.NbOfChannels,
            nameof(SongModelView.Encoder) => OriginalSong.Encoder,
            _ => typeof(SongModelView).GetProperty(propertyName)?.GetValue(OriginalSong)
        };
    }

    private object? GetCurrentValue(string? propertyName)
    {
        if (propertyName == null) return null;
        return propertyName switch
        {
            nameof(SongModelView.Title) => EditingSong.Title,
            nameof(SongModelView.TrackNumber) => EditingSong.TrackNumber,
            nameof(SongModelView.TrackTotal) => EditingSong.TrackTotal,
            nameof(SongModelView.DiscNumber) => EditingSong.DiscNumber,
            nameof(SongModelView.DiscTotal) => EditingSong.DiscTotal,
            nameof(SongModelView.ReleaseYear) => EditingSong.ReleaseYear,
            nameof(SongModelView.Conductor) => EditingSong.Conductor,
            nameof(SongModelView.Composer) => EditingSong.Composer,
            nameof(SongModelView.Lyricist) => EditingSong.Lyricist,
            nameof(SongModelView.BPM) => EditingSong.BPM,
            nameof(SongModelView.Language) => EditingSong.Language,
            nameof(SongModelView.Description) => EditingSong.Description,
            nameof(SongModelView.IsInstrumental) => EditingSong.IsInstrumental,
            nameof(SongModelView.GenreName) => EditingSong.GenreName,
            nameof(SongModelView.AlbumName) => EditingSong.AlbumName,
            nameof(SongModelView.CoverImagePath) => EditingSong.CoverImagePath,
            nameof(SongModelView.SyncLyrics) => EditingSong.SyncLyrics,
            nameof(SongModelView.UnSyncLyrics) => EditingSong.UnSyncLyrics,
            nameof(SongModelView.Rating) => EditingSong.Rating,
            nameof(SongModelView.IsFavorite) => EditingSong.IsFavorite,
            nameof(SongModelView.Achievement) => EditingSong.Achievement,
            nameof(SongModelView.DurationInSeconds) => EditingSong.DurationInSeconds,
            nameof(SongModelView.FilePath) => EditingSong.FilePath,
            nameof(SongModelView.FileFormat) => EditingSong.FileFormat,
            nameof(SongModelView.FileSize) => EditingSong.FileSize,
            nameof(SongModelView.BitRate) => EditingSong.BitRate,
            nameof(SongModelView.BitDepth) => EditingSong.BitDepth,
            nameof(SongModelView.SampleRate) => EditingSong.SampleRate,
            nameof(SongModelView.NbOfChannels) => EditingSong.NbOfChannels,
            nameof(SongModelView.Encoder) => EditingSong.Encoder,
            _ => typeof(SongModelView).GetProperty(propertyName)?.GetValue(EditingSong)
        };
    }
    private static bool AreValuesEqual(object? v1, object? v2)
    {
        if (v1 == null && v2 == null) return true;
        if (v1 == null || v2 == null) return false;

        if (v1 is string s1 && v2 is string s2)
            return string.Equals(s1.Trim(), s2.Trim(), StringComparison.Ordinal);

        if (v1 is float f1 && v2 is float f2)
            return Math.Abs(f1 - f2) < 0.001f;

        if (v1 is double d1 && v2 is double d2)
            return Math.Abs(d1 - d2) < 0.001;

        return v1.Equals(v2);
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
    private void AddOrUpdateChange(string propName, string displayName, string category, int order, object? oldVal, object? newVal)
    {
        if (_changeMap.TryGetValue(propName, out var change))
        {
            change.NewValue = newVal;
            change.IsAccepted = true;
        }
        else
        {
            var newChange = new PropertyChangeModelView
            {
                PropertyName = propName,
                DisplayName = displayName,
                Category = category,
                DisplayOrder = order,
                OldValue = oldVal,
                NewValue = newVal,
                IsAccepted = true
            };
            _changeMap[propName] = newChange;

            int index = PendingChanges.TakeWhile(c => c.DisplayOrder <= order).Count();
            PendingChanges.Insert(index, newChange);
        }

        TotalChangesCount = PendingChanges.Count;
    }


    private void RemoveChangeIfExists(string propName)
    {
        if (_changeMap.TryGetValue(propName, out var change))
        {
            PendingChanges.Remove(change);
            _changeMap.Remove(propName);
            TotalChangesCount = PendingChanges.Count;
        }
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
            var (Display, Category, Order) = TrackedProperties["Artists"];
            AddOrUpdateChange("Artists", Display, Category, Order, oldValue, newValue);
            //AddOrUpdateChange("Artists", oldValue, newValue,0);
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
    }

    #region Artist Management Commands

    [RelayCommand]
    public void FilterArtistSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            ArtistSuggestions.Clear();
            return;
        }

        var matches = AllArtistsMasterList
            .Where(a => a.Contains(query, StringComparison.OrdinalIgnoreCase) && !SelectedArtists.Contains(a))
            .Take(10)
            .ToList();

        ArtistSuggestions = new ObservableCollection<string>(matches);
    }

    [RelayCommand]
    public void AddArtist(string? artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName)) return;

        var clean = artistName.Trim();
        if (!SelectedArtists.Contains(clean, StringComparer.OrdinalIgnoreCase))
        {
            SelectedArtists.Add(clean);
            EvaluateArtistChanges();
        }
    }

    [RelayCommand]
    public void RemoveArtist(string? artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName)) return;

        var item = SelectedArtists.FirstOrDefault(a => string.Equals(a, artistName, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            SelectedArtists.Remove(item);
            EvaluateArtistChanges();
        }
    }

    private void EvaluateArtistChanges()
    {
        var current = SelectedArtists.OrderBy(a => a).ToList();
        var orig = _originalArtistNames.OrderBy(a => a).ToList();

        if (!current.SequenceEqual(orig, StringComparer.OrdinalIgnoreCase))
        {
            var oldVal = orig.Count != 0 ? string.Join(", ", orig) : "<None>";
            var newVal = current.Count != 0 ? string.Join(", ", current) : "<None>";
            AddOrUpdateChange("Artists", "Artists Linked", "Credits", 0, oldVal, newVal);
        }
        else
        {
            RemoveChangeIfExists("Artists");
        }

        UpdateHasChanges();
    }

    #endregion
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
    #region Cover Image Commands

    [RelayCommand]
    public async Task PickImageFromFileAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webp");

            var hwnd =  PlatUtils.DimmerHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                EditingSong.CoverImagePath = file.Path;
                ShowNotification("Cover image updated from local storage.", InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Failed to pick image: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public void ApplyAlbumCandidateImage(string imagePath)
    {
        if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
        {
            EditingSong.CoverImagePath = imagePath;
            ShowNotification("Applied album image.", InfoBarSeverity.Success);
        }
    }

    [RelayCommand]
    public void RemoveCoverImage()
    {
        EditingSong.CoverImagePath = string.Empty;
        ShowNotification("Cover image removed.", InfoBarSeverity.Informational);
    }

    #endregion

    [RelayCommand]
    public void DiscardChanges()
    {
        EditingSong = OriginalSong.ShallowCopy();

        SelectedArtists.Clear();
        foreach (var art in _originalArtistNames)
            SelectedArtists.Add(art);

        PendingChanges.Clear();
        _changeMap.Clear();
        UpdateHasChanges();
        TotalChangesCount = 0;

        EditingSong.PropertyChanged += OnEditingSongPropertyChanged;
        ShowNotification("Draft discarded. Restored original values.", InfoBarSeverity.Informational);
    }

    [RelayCommand]
    public async Task SaveChangesAsync()
    {
        if (!HasChanges)
        {
            ShowNotification("No changes detected to save.", InfoBarSeverity.Informational);
            return;
        }

        try
        {
            // 1. Commit editing values to OriginalSong
            foreach (var change in PendingChanges.Where(c => c.IsAccepted))
            {
                if (change.PropertyName == "Artists")
                {
                    await CommitArtistsToRealmAsync(SelectedArtists);
                    continue;
                }

                var prop = typeof(SongModelView).GetProperty(change.PropertyName);
                if (prop != null && prop.CanWrite)
                {
                    var val = prop.GetValue(EditingSong);
                    prop.SetValue(OriginalSong, val);
                }
            }

            // Sync denormalized / key fields
            OriginalSong.SetTitleAndDuration(OriginalSong.Title, OriginalSong.DurationInSeconds);
            OriginalSong.RefreshDenormalizedProperties();

            // 2. Commit to database through main VM
            await _mainViewModel.ApplyNewSongEdits(OriginalSong);

            // 3. Clear change tracking state
            PendingChanges.Clear();
            _changeMap.Clear();
            _originalArtistNames.Clear();
            _originalArtistNames.AddRange(SelectedArtists);

            UpdateHasChanges();
            TotalChangesCount = 0;

            ShowNotification("All modifications saved successfully.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification($"Failed to save changes: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    private async Task CommitArtistsToRealmAsync(IEnumerable<string> artistNames)
    {
        var realm = _realmFactory.GetRealmInstance();
        var songInDb = realm.Find<SongModel>(OriginalSong.Id);
        if (songInDb == null) return;

        await realm.WriteAsync(() =>
        {
            songInDb.ArtistToSong.Clear();
            foreach (var name in artistNames)
            {
                var existing = realm.All<ArtistModel>().FirstOrDefault(a => a.Name == name);
                if (existing == null)
                {
                    existing = realm.Add(new ArtistModel { Name = name });
                }
                songInDb.ArtistToSong.Add(existing);
            }

            // Update primary artist reference
            var primary = songInDb.ArtistToSong.FirstOrDefault();
            if (primary != null)
            {
                songInDb.Artist = primary;
                OriginalSong.ArtistName = primary.Name;
            }
        });
    }

    [RelayCommand]
    public async Task SearchOnlineAsync()
    {
        if (string.IsNullOrWhiteSpace(OnlineSearchTitle) && string.IsNullOrWhiteSpace(OnlineSearchArtist))
        {
            ShowNotification("Please provide a Title and Artist to search online.", InfoBarSeverity.Warning);
            return;
        }

        try
        {
            IsSearchingOnline = true;
            HasOnlineResults = false;

            var trackInfo = await _mainViewModel.LastfmService.GetTrackInfoAsync(OnlineSearchArtist, OnlineSearchTitle);
            if (trackInfo == null || trackInfo.IsNull)
            {
                ShowNotification("No online match found on Last.fm.", InfoBarSeverity.Informational);
                return;
            }

            OnlineMetadataResult = trackInfo;
            HasOnlineResults = true;
            ShowNotification("Online metadata retrieved successfully.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification($"Online search error: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            IsSearchingOnline = false;
        }
    }
    #region Online Metadata (Last.fm)

    [RelayCommand]
    public void ApplyOnlineMetadata()
    {
        if (OnlineMetadataResult == null) return;

        try
        {
            if (!string.IsNullOrWhiteSpace((string)OnlineMetadataResult.Name))
                EditingSong.Title = OnlineMetadataResult.Name;

            if (OnlineMetadataResult.Artist != null && !string.IsNullOrWhiteSpace((string)OnlineMetadataResult.Artist.Name))
            {
                var artistName = (string)OnlineMetadataResult.Artist.Name;
                if (!SelectedArtists.Contains(artistName))
                {
                    SelectedArtists.Clear();
                    SelectedArtists.Add(artistName);
                    EvaluateArtistChanges();
                }
            }

            if (OnlineMetadataResult.Album != null && !string.IsNullOrWhiteSpace((string)OnlineMetadataResult.Album.Name))
            {
                EditingSong.AlbumName = OnlineMetadataResult.Album.Name;
            }

            ShowNotification("Metadata merged into editing draft.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification($"Could not apply online metadata: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    #endregion



    private void ShowNotification(string message, InfoBarSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
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

            // PlayAsync stats (should probably not be editable, but just in case)
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

        if (acceptedChanges.Count == 0)
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

    private void SetOriginalValue(string propertyName, object? value)
    {
        switch (propertyName)
        {
            case nameof(SongModelView.Title):
                OriginalSong.Title = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.TrackNumber):
                OriginalSong.TrackNumber = value is int tn ? tn : 0;
                break;
            case nameof(SongModelView.TrackTotal):
                OriginalSong.TrackTotal = value is int tt ? tt : 0;
                break;
            case nameof(SongModelView.DiscNumber):
                OriginalSong.DiscNumber = value is int dn ? dn : 0;
                break;
            case nameof(SongModelView.DiscTotal):
                OriginalSong.DiscTotal = value is int dt ? dt : 0;
                break;
            case nameof(SongModelView.ReleaseYear):
                OriginalSong.ReleaseYear = value is int ry ? ry : 0;
                break;
            case nameof(SongModelView.IsInstrumental):
                OriginalSong.IsInstrumental = (bool)value!;
                break;
            case nameof(SongModelView.AlbumName):
                OriginalSong.AlbumName = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.GenreName):
                OriginalSong.GenreName = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.CoverImagePath):
                OriginalSong.CoverImagePath = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.Composer):
                OriginalSong.Composer = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.Conductor):
                OriginalSong.Conductor = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.Lyricist):
                OriginalSong.Lyricist = value as string ?? string.Empty;
                break;
            //case nameof(SongModelView.BPM):
                //OriginalSong.BPM = value is double bpm ? bpm : (value is double dBpm ? (double)dBpm : 0);
                //break;
            case nameof(SongModelView.Language):
                OriginalSong.Language = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.Description):
                OriginalSong.Description = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.SyncLyrics):
                OriginalSong.SyncLyrics = value as string;
                break;
            case nameof(SongModelView.UnSyncLyrics):
                OriginalSong.UnSyncLyrics = value as string;
                break;
            case nameof(SongModelView.Rating):
                OriginalSong.Rating = value is int r ? r : 0;
                break;
            case nameof(SongModelView.IsFavorite):
                OriginalSong.IsFavorite = value is bool fav && fav;
                break;
            case nameof(SongModelView.Achievement):
                OriginalSong.Achievement = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.DurationInSeconds):
                OriginalSong.DurationInSeconds = value is double dur ? dur : 0;
                break;
            case nameof(SongModelView.FilePath):
                OriginalSong.FilePath = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.FileFormat):
                OriginalSong.FileFormat = value as string ?? string.Empty;
                break;
            case nameof(SongModelView.FileSize):
                OriginalSong.FileSize = value is long fs ? fs : (value is int ifs ? ifs : 0L);
                break;
            case nameof(SongModelView.BitRate):
                OriginalSong.BitRate = value is int br ? br : 0;
                break;
            case nameof(SongModelView.BitDepth):
                OriginalSong.BitDepth = value is int bd ? bd : 0;
                break;
            case nameof(SongModelView.SampleRate):
                OriginalSong.SampleRate = value is double sr ? sr : 0;
                break;
            case nameof(SongModelView.NbOfChannels):
                OriginalSong.NbOfChannels = value is int nc ? nc : 0;
                break;
            case nameof(SongModelView.Encoder):
                OriginalSong.Encoder = value as string ?? string.Empty;
                break;
            default:
                var prop = typeof(SongModelView).GetProperty(propertyName);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(OriginalSong, value);
                }
                break;
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
        SelectedArtists = new ObservableCollection<string?>(_originalArtistNames);

        // Clear changes
        PendingChanges.Clear();
        _changeMap.Clear();
        UpdateHasChanges();
    }
}