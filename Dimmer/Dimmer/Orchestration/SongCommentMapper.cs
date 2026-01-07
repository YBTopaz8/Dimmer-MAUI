namespace Dimmer.Orchestration;

/// <summary>
/// Helper class for mapping between Realm UserNoteModel and Parse SongComment
/// </summary>
public static class SongCommentMapper
{
    /// <summary>
    /// Convert a Realm UserNoteModel to a Parse SongComment for syncing
    /// </summary>
    public static SongComment ToSongComment(UserNoteModel note, string songId, string songTitle, string artistName)
    {
        var comment = new SongComment
        {
            SongId = songId,
            SongTitle = songTitle,
            ArtistName = artistName,
            Text = note.UserMessageText ?? string.Empty,
            IsPublic = note.IsPublic,
            TimestampMs = note.TimestampMs,
            IsPinned = note.IsPinned,
            UserRating = note.UserRating,
            MessageColor = note.MessageColor,
            ImagePath = note.UserMessageImagePath,
            AudioPath = note.UserMessageAudioPath
        };

        // Parse reactions from JSON string
        if (!string.IsNullOrEmpty(note.ReactionsJson))
        {
            try
            {
                var reactions = ParseReactionsFromJson(note.ReactionsJson);
                comment.Reactions = reactions;
            }
            catch (Exception)
            {
                // If parsing fails, use empty dictionary
                comment.Reactions = new Dictionary<string, int>();
            }
        }
        else
        {
            comment.Reactions = new Dictionary<string, int>();
        }

        comment.ReactionUsers = new Dictionary<string, IList<string>>();

        return comment;
    }

    /// <summary>
    /// Convert a Parse SongComment to Realm UserNoteModel for local storage
    /// </summary>
    public static UserNoteModel ToUserNoteModel(SongComment comment)
    {
        var note = new UserNoteModel
        {
            Id = comment.ObjectId,
            UserMessageText = comment.Text,
            IsPublic = comment.IsPublic,
            TimestampMs = comment.TimestampMs,
            IsPinned = comment.IsPinned,
            UserRating = comment.UserRating,
            MessageColor = comment.MessageColor,
            UserMessageImagePath = comment.ImagePath,
            UserMessageAudioPath = comment.AudioPath,
            CreatedAt = comment.CreatedAt,
            ModifiedAt = comment.UpdatedAt ?? comment.CreatedAt,
            AuthorId = comment.Author?.ObjectId,
            AuthorUsername = comment.Author?.Username
        };

        // Convert reactions to JSON string
        if (comment.Reactions != null && comment.Reactions.Any())
        {
            note.ReactionsJson = SerializeReactionsToJson(comment.Reactions);
        }

        return note;
    }

    /// <summary>
    /// Convert a Parse SongComment to UserNoteModelView for UI
    /// </summary>
    public static UserNoteModelView ToUserNoteModelView(SongComment comment)
    {
        var view = new UserNoteModelView
        {
            Id = comment.ObjectId,
            UserMessageText = comment.Text,
            IsPublic = comment.IsPublic,
            TimestampMs = comment.TimestampMs,
            IsPinned = comment.IsPinned,
            UserRating = comment.UserRating,
            MessageColor = comment.MessageColor,
            UserMessageImagePath = comment.ImagePath,
            UserMessageAudioPath = comment.AudioPath,
            CreatedAt = comment.CreatedAt,
            ModifiedAt = comment.UpdatedAt ?? comment.CreatedAt,
            AuthorId = comment.Author?.ObjectId,
            AuthorUsername = comment.Author?.Username,
            Reactions = comment.Reactions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>()
        };

        return view;
    }

    /// <summary>
    /// Parse reactions from simple JSON string format: "like:12,fire:3,sad:1"
    /// </summary>
    private static Dictionary<string, int> ParseReactionsFromJson(string json)
    {
        var reactions = new Dictionary<string, int>();

        if (string.IsNullOrWhiteSpace(json))
            return reactions;

        var pairs = json.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[1], out int count))
            {
                reactions[parts[0].Trim()] = count;
            }
        }

        return reactions;
    }

    /// <summary>
    /// Serialize reactions to simple JSON string format: "like:12,fire:3,sad:1"
    /// </summary>
    private static string SerializeReactionsToJson(IDictionary<string, int> reactions)
    {
        if (reactions == null || !reactions.Any())
            return string.Empty;

        return string.Join(",", reactions.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
    }

    /// <summary>
    /// Update a UserNoteModelView with data from a SongComment
    /// </summary>
    public static void UpdateUserNoteModelView(UserNoteModelView view, SongComment comment)
    {
        view.UserMessageText = comment.Text;
        view.IsPublic = comment.IsPublic;
        view.TimestampMs = comment.TimestampMs;
        view.IsPinned = comment.IsPinned;
        view.UserRating = comment.UserRating;
        view.MessageColor = comment.MessageColor;
        view.UserMessageImagePath = comment.ImagePath;
        view.UserMessageAudioPath = comment.AudioPath;
        view.ModifiedAt = comment.UpdatedAt ?? comment.CreatedAt;
        view.AuthorId = comment.Author?.ObjectId;
        view.AuthorUsername = comment.Author?.Username;
        view.Reactions = comment.Reactions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, int>();
    }
}
