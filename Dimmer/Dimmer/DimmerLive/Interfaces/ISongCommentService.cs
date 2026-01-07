namespace Dimmer.DimmerLive.Interfaces;

/// <summary>
/// Service interface for managing song comments with Parse backend integration
/// </summary>
public interface ISongCommentService
{
    /// <summary>
    /// Observable stream of all song comments (filtered by current query)
    /// </summary>
    IObservable<IChangeSet<SongComment, string>> Comments { get; }

    /// <summary>
    /// Get all public comments for a specific song
    /// </summary>
    /// <param name="songId">The song's ObjectId</param>
    /// <returns>List of public comments</returns>
    Task<IEnumerable<SongComment>> GetPublicCommentsForSongAsync(string songId);

    /// <summary>
    /// Get user's private comments for a specific song
    /// </summary>
    /// <param name="songId">The song's ObjectId</param>
    /// <param name="userId">The user's ObjectId (current user if null)</param>
    /// <returns>List of private comments</returns>
    Task<IEnumerable<SongComment>> GetPrivateCommentsForSongAsync(string songId, string? userId = null);

    /// <summary>
    /// Get all comments (public + user's private) for a song
    /// </summary>
    /// <param name="songId">The song's ObjectId</param>
    /// <returns>List of all relevant comments</returns>
    Task<IEnumerable<SongComment>> GetAllCommentsForSongAsync(string songId);

    /// <summary>
    /// Create a new comment
    /// </summary>
    /// <param name="songId">Song ObjectId</param>
    /// <param name="text">Comment text</param>
    /// <param name="isPublic">Whether the comment is public</param>
    /// <param name="timestampMs">Optional timestamp in milliseconds</param>
    /// <returns>The created comment</returns>
    Task<SongComment?> CreateCommentAsync(string songId, string text, bool isPublic, int? timestampMs = null);

    /// <summary>
    /// Update an existing comment
    /// </summary>
    /// <param name="commentId">Comment ObjectId</param>
    /// <param name="text">New text</param>
    /// <param name="isPublic">New public/private setting</param>
    /// <returns>Updated comment</returns>
    Task<SongComment?> UpdateCommentAsync(string commentId, string text, bool isPublic);

    /// <summary>
    /// Delete a comment (user can only delete their own)
    /// </summary>
    /// <param name="commentId">Comment ObjectId</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteCommentAsync(string commentId);

    /// <summary>
    /// Add a reaction to a comment
    /// </summary>
    /// <param name="commentId">Comment ObjectId</param>
    /// <param name="reactionType">Type of reaction (e.g., "like", "fire", "sad")</param>
    /// <returns>Updated comment</returns>
    Task<SongComment?> AddReactionAsync(string commentId, string reactionType);

    /// <summary>
    /// Remove a reaction from a comment
    /// </summary>
    /// <param name="commentId">Comment ObjectId</param>
    /// <param name="reactionType">Type of reaction to remove</param>
    /// <returns>Updated comment</returns>
    Task<SongComment?> RemoveReactionAsync(string commentId, string reactionType);

    /// <summary>
    /// Toggle a reaction on a comment (add if not present, remove if present)
    /// </summary>
    /// <param name="commentId">Comment ObjectId</param>
    /// <param name="reactionType">Type of reaction</param>
    /// <returns>Updated comment</returns>
    Task<SongComment?> ToggleReactionAsync(string commentId, string reactionType);

    /// <summary>
    /// Subscribe to live updates for a specific song's comments
    /// </summary>
    /// <param name="songId">Song ObjectId</param>
    void SubscribeToSongComments(string songId);

    /// <summary>
    /// Unsubscribe from live updates
    /// </summary>
    void UnsubscribeFromComments();

    /// <summary>
    /// Sync local Realm UserNotes with Parse comments
    /// Uploads private notes to Parse and downloads public comments
    /// </summary>
    /// <param name="song">The song to sync</param>
    Task SyncCommentsForSongAsync(SongModelView song);
}
