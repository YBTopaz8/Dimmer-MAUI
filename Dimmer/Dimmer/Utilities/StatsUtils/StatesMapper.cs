namespace Dimmer.Utilities.StatsUtils;

public static class StatesMapper
{
    public static DimmerPlaybackState? Map(PlayType playType)
    {
        return playType switch
        {
            PlayType.Play => DimmerPlaybackState.Playing,
            PlayType.Pause => DimmerPlaybackState.PausedUser,
            PlayType.Resume => DimmerPlaybackState.Resumed,
            PlayType.Completed => DimmerPlaybackState.PlayCompleted,
            PlayType.Seeked => DimmerPlaybackState.Buffering,
            PlayType.Skipped => DimmerPlaybackState.Skipped,
            PlayType.Restarted => DimmerPlaybackState.Playing,
            PlayType.SeekRestarted => DimmerPlaybackState.Buffering,
            PlayType.CustomRepeat => DimmerPlaybackState.RepeatSame,
            PlayType.Previous => DimmerPlaybackState.PlayPreviousUser,
            PlayType.ShareSong => DimmerPlaybackState.None,
            PlayType.ReceiveShare => DimmerPlaybackState.None,
            // all chat/log events don’t map to playback states:
            PlayType.LogEvent => DimmerPlaybackState.None,
            PlayType.ChatSent => DimmerPlaybackState.None,
            PlayType.ChatReceived => DimmerPlaybackState.None,
            PlayType.ChatDeleted => DimmerPlaybackState.None,
            PlayType.ChatEdited => DimmerPlaybackState.None,
            PlayType.ChatPinned => DimmerPlaybackState.None,
            PlayType.ChatUnpinned => DimmerPlaybackState.None,
            PlayType.ChatLiked => DimmerPlaybackState.None,
            PlayType.ChatUnliked => DimmerPlaybackState.None,
            PlayType.ChatShared => DimmerPlaybackState.None,
            PlayType.ChatUnread => DimmerPlaybackState.None,
            PlayType.ChatRead => DimmerPlaybackState.None,
            PlayType.ChatMentioned => DimmerPlaybackState.None,
            PlayType.ChatUnmentioned => DimmerPlaybackState.None,
            PlayType.ChatReplied => DimmerPlaybackState.None,
            PlayType.ChatUnreplied => DimmerPlaybackState.None,
            PlayType.ChatForwarded => DimmerPlaybackState.None,
            PlayType.ChatUnforwarded => DimmerPlaybackState.None,
            PlayType.ChatSaved => DimmerPlaybackState.None,
            PlayType.ChatUnsaved => DimmerPlaybackState.None,
            PlayType.ChatReported => DimmerPlaybackState.None,
            PlayType.ChatUnreported => DimmerPlaybackState.None,
            PlayType.ChatBlocked => DimmerPlaybackState.None,
            PlayType.ChatUnblocked => DimmerPlaybackState.None,
            PlayType.ChatMuted => DimmerPlaybackState.None,
            PlayType.ChatUnmuted => DimmerPlaybackState.None,
            PlayType.ChatPinnedMessage => DimmerPlaybackState.None,
            PlayType.AddToPlaylist => DimmerPlaybackState.PlaylistPlay,

            _ => null
        };
    }

    public static PlayType? Map(DimmerPlaybackState state)
    {
        return state switch
        {
            DimmerPlaybackState.Playing => PlayType.Play,
            DimmerPlaybackState.PlaylistPlay => PlayType.Play,
            DimmerPlaybackState.Resumed => PlayType.Resume,
            DimmerPlaybackState.PausedUser => PlayType.Pause,
            DimmerPlaybackState.PlayCompleted => PlayType.Completed,
            DimmerPlaybackState.Buffering => PlayType.Seeked,
            DimmerPlaybackState.Skipped => PlayType.Skipped,
            DimmerPlaybackState.RepeatSame => PlayType.CustomRepeat,
            DimmerPlaybackState.PlayPreviousUser => PlayType.Previous,
            DimmerPlaybackState.PausedDimmer => PlayType.Pause,
            DimmerPlaybackState.Seeked => PlayType.Seeked,
            DimmerPlaybackState.Favorited => PlayType.Favorited,
            // states that have no meaningful PlayType:

            _ => null

        };
    }
}