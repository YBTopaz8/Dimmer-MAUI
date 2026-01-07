namespace Dimmer.ViewModel;

/// <summary>
/// ViewModel for managing song comments with public/private visibility and reactions
/// </summary>
public partial class SongCommentsViewModel : ObservableObject, IDisposable
{
    private readonly ISongCommentService _commentService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<SongCommentsViewModel>? _logger;
    private readonly CompositeDisposable _disposables = new();
    private readonly BaseViewModel _baseViewModel;

    [ObservableProperty]
    private ObservableCollection<SongCommentView> _publicComments = new();

    [ObservableProperty]
    private ObservableCollection<SongCommentView> _privateComments = new();

    [ObservableProperty]
    private ObservableCollection<SongCommentView> _allComments = new();

    [ObservableProperty]
    private SongModelView? _currentSong;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _newCommentText = string.Empty;

    [ObservableProperty]
    private bool _newCommentIsPublic = true;

    [ObservableProperty]
    private int? _newCommentTimestamp;

    [ObservableProperty]
    private string? _currentUserId;

    [ObservableProperty]
    private string? _currentUsername;

    public SongCommentsViewModel(
        ISongCommentService commentService,
        IAuthenticationService authService,
        BaseViewModel baseViewModel,
        ILogger<SongCommentsViewModel>? logger = null)
    {
        _commentService = commentService;
        _authService = authService;
        _baseViewModel = baseViewModel;
        _logger = logger;

        // Subscribe to authentication state
        _authService.CurrentUser
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(user =>
            {
                IsAuthenticated = user != null;
                CurrentUserId = user?.ObjectId;
                CurrentUsername = user?.Username;
            })
            .DisposeWith(_disposables);

        // Subscribe to comment changes
        _commentService.Comments
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(changes =>
            {
                RefreshComments();
            })
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Load comments for a specific song
    /// </summary>
    [RelayCommand]
    private async Task LoadCommentsForSongAsync(SongModelView song)
    {
        if (song == null) return;

        try
        {
            IsLoading = true;
            CurrentSong = song;

            var songId = song.Id.ToString();

            // Subscribe to live updates for this song
            _commentService.SubscribeToSongComments(songId);

            // Fetch all comments
            var comments = await _commentService.GetAllCommentsForSongAsync(songId);

            // Convert to view models
            var commentViews = comments.Select(c => new SongCommentView(c)).ToList();

            // Separate public and private
            PublicComments = new ObservableCollection<SongCommentView>(
                commentViews.Where(c => c.IsPublic).OrderByDescending(c => c.IsPinned).ThenBy(c => c.TimestampMs ?? int.MaxValue)
            );

            PrivateComments = new ObservableCollection<SongCommentView>(
                commentViews.Where(c => !c.IsPublic).OrderBy(c => c.TimestampMs ?? int.MaxValue)
            );

            AllComments = new ObservableCollection<SongCommentView>(
                commentViews.OrderByDescending(c => c.IsPinned).ThenBy(c => c.TimestampMs ?? int.MaxValue)
            );

            _logger?.LogInformation("Loaded {Count} comments for song {SongTitle}", commentViews.Count, song.Title);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load comments for song");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Create a new comment
    /// </summary>
    [RelayCommand]
    private async Task CreateCommentAsync()
    {
        if (CurrentSong == null || string.IsNullOrWhiteSpace(NewCommentText))
        {
            _logger?.LogWarning("Cannot create comment: missing song or text");
            return;
        }

        try
        {
            IsLoading = true;

            var songId = CurrentSong.Id.ToString();
            var comment = await _commentService.CreateCommentAsync(
                songId,
                NewCommentText,
                NewCommentIsPublic,
                NewCommentTimestamp
            );

            if (comment != null)
            {
                // Clear form
                NewCommentText = string.Empty;
                NewCommentTimestamp = null;

                // Refresh list
                await LoadCommentsForSongAsync(CurrentSong);

                _logger?.LogInformation("Created comment successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create comment");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Update an existing comment
    /// </summary>
    [RelayCommand]
    private async Task UpdateCommentAsync(SongCommentView commentView)
    {
        if (commentView == null) return;

        try
        {
            IsLoading = true;

            var updated = await _commentService.UpdateCommentAsync(
                commentView.ObjectId,
                commentView.Text,
                commentView.IsPublic
            );

            if (updated != null)
            {
                await LoadCommentsForSongAsync(CurrentSong!);
                _logger?.LogInformation("Updated comment successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to update comment");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    [RelayCommand]
    private async Task DeleteCommentAsync(SongCommentView commentView)
    {
        if (commentView == null) return;

        try
        {
            IsLoading = true;

            var success = await _commentService.DeleteCommentAsync(commentView.ObjectId);

            if (success)
            {
                await LoadCommentsForSongAsync(CurrentSong!);
                _logger?.LogInformation("Deleted comment successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete comment");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggle a reaction on a comment
    /// </summary>
    [RelayCommand]
    private async Task ToggleReactionAsync((string commentId, string reactionType) args)
    {
        try
        {
            var updated = await _commentService.ToggleReactionAsync(args.commentId, args.reactionType);

            if (updated != null)
            {
                // Update the local view model
                var commentView = AllComments.FirstOrDefault(c => c.ObjectId == args.commentId);
                if (commentView != null)
                {
                    commentView.Reactions = updated.Reactions;
                    commentView.OnPropertyChanged(nameof(commentView.TotalReactions));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to toggle reaction");
        }
    }

    /// <summary>
    /// Handle timestamp click - seek to the timestamp in the song
    /// </summary>
    [RelayCommand]
    private async Task OnTimestampClickAsync(int timestampMs)
    {
        try
        {
            if (CurrentSong == null) return;

            // Seek to the timestamp
            var positionInSeconds = timestampMs / 1000.0;

            // Use BaseViewModel's SeekTrackPosition to navigate to the timestamp
            _baseViewModel.SeekTrackPosition(positionInSeconds);

            _logger?.LogInformation("Seeking to timestamp {TimestampMs}ms", timestampMs);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to seek to timestamp");
        }
    }

    /// <summary>
    /// Set the timestamp for a new comment from current playback position
    /// </summary>
    [RelayCommand]
    private void SetTimestampFromCurrentPosition()
    {
        try
        {
            var currentPositionSeconds = _baseViewModel.AudioService.CurrentPosition;
            NewCommentTimestamp = (int)(currentPositionSeconds * 1000);
            _logger?.LogInformation("Set timestamp to {Timestamp}ms", NewCommentTimestamp);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to set timestamp");
        }
    }

    /// <summary>
    /// Clear the timestamp for a new comment
    /// </summary>
    [RelayCommand]
    private void ClearTimestamp()
    {
        NewCommentTimestamp = null;
    }

    /// <summary>
    /// Sync local notes with Parse
    /// </summary>
    [RelayCommand]
    private async Task SyncCommentsAsync()
    {
        if (CurrentSong == null) return;

        try
        {
            IsLoading = true;
            await _commentService.SyncCommentsForSongAsync(CurrentSong);
            await LoadCommentsForSongAsync(CurrentSong);
            _logger?.LogInformation("Synced comments successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to sync comments");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshComments()
    {
        if (CurrentSong != null)
        {
            _ = LoadCommentsForSongAsync(CurrentSong);
        }
    }

    public void Dispose()
    {
        _commentService.UnsubscribeFromComments();
        _disposables.Dispose();
    }
}

/// <summary>
/// View model wrapper for SongComment Parse objects
/// </summary>
public partial class SongCommentView : ObservableObject
{
    private readonly SongComment _comment;

    public string ObjectId => _comment.ObjectId;

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private bool _isPublic;

    [ObservableProperty]
    private int? _timestampMs;

    [ObservableProperty]
    private string _authorId;

    [ObservableProperty]
    private string _authorUsername;

    [ObservableProperty]
    private Dictionary<string, int> _reactions;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private DateTimeOffset _createdAt;

    public string TimestampDisplay
    {
        get
        {
            if (TimestampMs == null) return string.Empty;
            var ts = TimeSpan.FromMilliseconds(TimestampMs.Value);
            return ts.Hours > 0
                ? ts.ToString(@"hh\:mm\:ss")
                : ts.ToString(@"mm\:ss");
        }
    }

    public int TotalReactions => Reactions?.Values.Sum() ?? 0;

    public SongCommentView(SongComment comment)
    {
        _comment = comment;
        Text = comment.Text;
        IsPublic = comment.IsPublic;
        TimestampMs = comment.TimestampMs;
        AuthorId = comment.Author?.ObjectId ?? string.Empty;
        AuthorUsername = comment.Author?.Username ?? "Unknown";
        Reactions = comment.Reactions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>();
        IsPinned = comment.IsPinned;
        CreatedAt = comment.CreatedAt;
    }
}
