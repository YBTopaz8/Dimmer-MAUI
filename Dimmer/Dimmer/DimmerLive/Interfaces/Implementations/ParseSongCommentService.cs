using Parse.LiveQuery;

namespace Dimmer.DimmerLive.Interfaces.Implementations;

/// <summary>
/// Parse-backed implementation of song comment service with live query support
/// </summary>
public partial class ParseSongCommentService : ObservableObject, ISongCommentService, IDisposable
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ParseSongCommentService> _logger;
    private readonly ParseLiveQueryClient _liveQueryClient;
    private readonly IRepository<SongModel> _songRepository;

    private readonly SourceCache<SongComment, string> _commentsCache = new(c => c.ObjectId);
    private Subscription<SongComment>? _commentSubscription;
    private readonly CompositeDisposable _disposables = new();

    public IObservable<IChangeSet<SongComment, string>> Comments => _commentsCache.Connect();

    public ParseSongCommentService(
        IAuthenticationService authService,
        ILogger<ParseSongCommentService> logger,
        ParseLiveQueryClient liveQueryClient,
        IRepository<SongModel> songRepository)
    {
        _authService = authService;
        _logger = logger;
        _liveQueryClient = liveQueryClient;
        _songRepository = songRepository;
    }

    public async Task<IEnumerable<SongComment>> GetPublicCommentsForSongAsync(string songId)
    {
        try
        {
            var query = ParseClient.Instance.GetQuery<SongComment>()
                .WhereEqualTo(nameof(SongComment.SongId), songId)
                .WhereEqualTo(nameof(SongComment.IsPublic), true)
                .Include(nameof(SongComment.Author))
                .OrderByDescending(nameof(SongComment.CreatedAt));

            var results = await query.FindAsync();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch public comments for song {SongId}", songId);
            return Enumerable.Empty<SongComment>();
        }
    }

    public async Task<IEnumerable<SongComment>> GetPrivateCommentsForSongAsync(string songId, string? userId = null)
    {
        try
        {
            var currentUser = await ParseUser.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user for private comments");
                return Enumerable.Empty<SongComment>();
            }

            var targetUserId = userId ?? currentUser.ObjectId;

            var query = ParseClient.Instance.GetQuery<SongComment>()
                .WhereEqualTo(nameof(SongComment.SongId), songId)
                .WhereEqualTo(nameof(SongComment.IsPublic), false)
                .WhereEqualTo(nameof(SongComment.Author), ParseUser.CreateWithoutData(targetUserId))
                .Include(nameof(SongComment.Author))
                .OrderByDescending(nameof(SongComment.CreatedAt));

            var results = await query.FindAsync();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch private comments for song {SongId}", songId);
            return Enumerable.Empty<SongComment>();
        }
    }

    public async Task<IEnumerable<SongComment>> GetAllCommentsForSongAsync(string songId)
    {
        try
        {
            var publicComments = await GetPublicCommentsForSongAsync(songId);
            var privateComments = await GetPrivateCommentsForSongAsync(songId);

            return publicComments.Concat(privateComments)
                .OrderByDescending(c => c.IsPinned)
                .ThenBy(c => c.TimestampMs ?? int.MaxValue)
                .ThenByDescending(c => c.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch all comments for song {SongId}", songId);
            return Enumerable.Empty<SongComment>();
        }
    }

    public async Task<SongComment?> CreateCommentAsync(string songId, string text, bool isPublic, int? timestampMs = null)
    {
        try
        {
            var currentUser = await ParseUser.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user to create comment");
                return null;
            }

            // Get song details for metadata
            var song = _songRepository.GetAll().FirstOrDefault(s => s.Id.ToString() == songId);

            var comment = new SongComment
            {
                SongId = songId,
                SongTitle = song?.Title ?? "Unknown",
                ArtistName = song?.ArtistName ?? "Unknown",
                Author = currentUser,
                Text = text,
                IsPublic = isPublic,
                TimestampMs = timestampMs,
                Reactions = new Dictionary<string, int>(),
                ReactionUsers = new Dictionary<string, IList<string>>(),
                IsPinned = false,
                UserRating = 0
            };

            // Set ACL: public read if isPublic, otherwise only author can read/write
            var acl = new ParseACL(currentUser);
            if (isPublic)
            {
                acl.PublicReadAccess = true;
            }
            comment.ACL = acl;

            await comment.SaveAsync();
            _logger.LogInformation("Created comment {CommentId} for song {SongId}", comment.ObjectId, songId);

            return comment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create comment for song {SongId}", songId);
            return null;
        }
    }

    public async Task<SongComment?> UpdateCommentAsync(string commentId, string text, bool isPublic)
    {
        try
        {
            var query = ParseClient.Instance.GetQuery<SongComment>()
                .WhereEqualTo("objectId", commentId);

            var comment = await query.FirstOrDefaultAsync();
            if (comment == null)
            {
                _logger.LogWarning("Comment {CommentId} not found", commentId);
                return null;
            }

            comment.Text = text;
            comment.IsPublic = isPublic;

            // Update ACL if visibility changed
            var currentUser = await ParseUser.GetCurrentUserAsync();
            if (currentUser != null)
            {
                var acl = new ParseACL(currentUser);
                if (isPublic)
                {
                    acl.PublicReadAccess = true;
                }
                comment.ACL = acl;
            }

            await comment.SaveAsync();
            _logger.LogInformation("Updated comment {CommentId}", commentId);

            return comment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update comment {CommentId}", commentId);
            return null;
        }
    }

    public async Task<bool> DeleteCommentAsync(string commentId)
    {
        try
        {
            var query = ParseClient.Instance.GetQuery<SongComment>()
                .WhereEqualTo("objectId", commentId);

            var comment = await query.FirstOrDefaultAsync();
            if (comment == null)
            {
                _logger.LogWarning("Comment {CommentId} not found", commentId);
                return false;
            }

            await comment.DeleteAsync();
            _logger.LogInformation("Deleted comment {CommentId}", commentId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete comment {CommentId}", commentId);
            return false;
        }
    }

    public async Task<SongComment?> AddReactionAsync(string commentId, string reactionType)
    {
        try
        {
            var currentUser = await ParseUser.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user for reaction");
                return null;
            }

            var query = ParseClient.Instance.GetQuery<SongComment>()
                .WhereEqualTo("objectId", commentId);

            var comment = await query.FirstOrDefaultAsync();
            if (comment == null)
            {
                _logger.LogWarning("Comment {CommentId} not found", commentId);
                return null;
            }

            var reactions = comment.Reactions ?? new Dictionary<string, int>();
            var reactionUsers = comment.ReactionUsers ?? new Dictionary<string, IList<string>>();

            // Check if user already reacted with this type
            if (!reactionUsers.ContainsKey(reactionType))
            {
                reactionUsers[reactionType] = new List<string>();
            }

            if (reactionUsers[reactionType].Contains(currentUser.ObjectId))
            {
                _logger.LogInformation("User already reacted with {ReactionType}", reactionType);
                return comment;
            }

            // Add reaction
            reactionUsers[reactionType].Add(currentUser.ObjectId);
            reactions[reactionType] = reactions.ContainsKey(reactionType) 
                ? reactions[reactionType] + 1 
                : 1;

            comment.Reactions = reactions;
            comment.ReactionUsers = reactionUsers;

            await comment.SaveAsync();
            _logger.LogInformation("Added {ReactionType} reaction to comment {CommentId}", reactionType, commentId);

            return comment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add reaction to comment {CommentId}", commentId);
            return null;
        }
    }

    public async Task<SongComment?> RemoveReactionAsync(string commentId, string reactionType)
    {
        try
        {
            var currentUser = await ParseUser.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user for reaction removal");
                return null;
            }

            var query = ParseClient.Instance.GetQuery<SongComment>()
                .WhereEqualTo("objectId", commentId);

            var comment = await query.FirstOrDefaultAsync();
            if (comment == null)
            {
                _logger.LogWarning("Comment {CommentId} not found", commentId);
                return null;
            }

            var reactions = comment.Reactions ?? new Dictionary<string, int>();
            var reactionUsers = comment.ReactionUsers ?? new Dictionary<string, IList<string>>();

            if (!reactionUsers.ContainsKey(reactionType) || !reactionUsers[reactionType].Contains(currentUser.ObjectId))
            {
                _logger.LogInformation("User has not reacted with {ReactionType}", reactionType);
                return comment;
            }

            // Remove reaction
            reactionUsers[reactionType].Remove(currentUser.ObjectId);
            reactions[reactionType] = Math.Max(0, reactions[reactionType] - 1);

            // Clean up empty entries
            if (reactions[reactionType] == 0)
            {
                reactions.Remove(reactionType);
            }
            if (reactionUsers[reactionType].Count == 0)
            {
                reactionUsers.Remove(reactionType);
            }

            comment.Reactions = reactions;
            comment.ReactionUsers = reactionUsers;

            await comment.SaveAsync();
            _logger.LogInformation("Removed {ReactionType} reaction from comment {CommentId}", reactionType, commentId);

            return comment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove reaction from comment {CommentId}", commentId);
            return null;
        }
    }

    public async Task<SongComment?> ToggleReactionAsync(string commentId, string reactionType)
    {
        try
        {
            var currentUser = await ParseUser.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user for reaction toggle");
                return null;
            }

            var query = ParseClient.Instance.GetQuery<SongComment>()
                .WhereEqualTo("objectId", commentId);

            var comment = await query.FirstOrDefaultAsync();
            if (comment == null)
            {
                _logger.LogWarning("Comment {CommentId} not found", commentId);
                return null;
            }

            var reactionUsers = comment.ReactionUsers ?? new Dictionary<string, IList<string>>();

            // Check if user already has this reaction
            if (reactionUsers.ContainsKey(reactionType) && reactionUsers[reactionType].Contains(currentUser.ObjectId))
            {
                return await RemoveReactionAsync(commentId, reactionType);
            }
            else
            {
                return await AddReactionAsync(commentId, reactionType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle reaction on comment {CommentId}", commentId);
            return null;
        }
    }

    public void SubscribeToSongComments(string songId)
    {
        try
        {
            // Unsubscribe from previous subscription
            UnsubscribeFromComments();

            var query = ParseClient.Instance.GetQuery<SongComment>()
                .WhereEqualTo(nameof(SongComment.SongId), songId)
                .Include(nameof(SongComment.Author));

            _commentSubscription = _liveQueryClient.Subscribe(query);

            _commentSubscription.On(Subscription.Event.Create, comment =>
            {
                _commentsCache.AddOrUpdate(comment);
                _logger.LogDebug("Comment created: {CommentId}", comment.ObjectId);
            });

            _commentSubscription.On(Subscription.Event.Update, comment =>
            {
                _commentsCache.AddOrUpdate(comment);
                _logger.LogDebug("Comment updated: {CommentId}", comment.ObjectId);
            });

            _commentSubscription.On(Subscription.Event.Delete, comment =>
            {
                _commentsCache.Remove(comment);
                _logger.LogDebug("Comment deleted: {CommentId}", comment.ObjectId);
            });

            _logger.LogInformation("Subscribed to comments for song {SongId}", songId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to comments for song {SongId}", songId);
        }
    }

    public void UnsubscribeFromComments()
    {
        try
        {
            if (_commentSubscription != null)
            {
                _liveQueryClient.Unsubscribe(_commentSubscription);
                _commentSubscription = null;
                _commentsCache.Clear();
                _logger.LogInformation("Unsubscribed from comments");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from comments");
        }
    }

    public async Task SyncCommentsForSongAsync(SongModelView song)
    {
        try
        {
            var currentUser = await ParseUser.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user for sync");
                return;
            }

            var songId = song.Id.ToString();

            // Download public comments from Parse
            var publicComments = await GetPublicCommentsForSongAsync(songId);

            // Upload local private notes to Parse (if they don't exist yet)
            if (song.UserNoteAggregatedCol?.Any() == true)
            {
                foreach (var note in song.UserNoteAggregatedCol.Where(n => !n.IsPublic))
                {
                    // Check if this note already exists in Parse
                    var existingQuery = ParseClient.Instance.GetQuery<SongComment>()
                        .WhereEqualTo(nameof(SongComment.SongId), songId)
                        .WhereEqualTo(nameof(SongComment.Author), currentUser);

                    var existing = await existingQuery.FindAsync();
                    
                    var noteExists = existing.Any(c => 
                        c.Text == note.UserMessageText && 
                        c.TimestampMs == note.TimestampMs);

                    if (!noteExists)
                    {
                        await CreateCommentAsync(
                            songId,
                            note.UserMessageText ?? string.Empty,
                            note.IsPublic,
                            note.TimestampMs
                        );
                    }
                }
            }

            _logger.LogInformation("Synced comments for song {SongId}", songId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync comments for song {SongId}", song.Id);
        }
    }

    public void Dispose()
    {
        UnsubscribeFromComments();
        _disposables.Dispose();
        _commentsCache.Dispose();
    }
}
