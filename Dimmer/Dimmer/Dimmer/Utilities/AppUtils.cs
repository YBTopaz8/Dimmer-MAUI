namespace Dimmer.Utilities;
public static class AppUtils
{
    public static int UserScreenHeight { get; set; }
    public static int UserScreenWidth { get; set; }

    public static bool IsUserFirstTimeOpening { get; set; } = false;


    public static class UserFriendlyLogGenerator
    {
        public static string GetPlaybackStateMessage(PlayType? type, SongModelView? currentSong, double? position = null)
        {
            if (type == null || currentSong == null)
            {
                return "Playback state is unknown or song information is missing.";
            }
            // Gracefully handle if currentSong or its Title is null/empty
            string songTitle = !string.IsNullOrWhiteSpace(currentSong?.Title) ? $"\"{currentSong.Title}\"" : "the current track";
            string artistName = !string.IsNullOrWhiteSpace(currentSong?.ArtistName) ? $" by {currentSong.ArtistName}" : ""; // Optional: Add artist if available

            // Combine title and artist for a richer description
            string fullSongDescription = $"{songTitle}{artistName}";

            switch (type)
            {
                case PlayType.Play:
                    return $"Now playing: {fullSongDescription}.";
                case PlayType.Pause:
                    return $"Paused: {fullSongDescription}.";
                case PlayType.Resume:
                    return $"Resumed: {fullSongDescription}.";
                case PlayType.Completed:
                    return $"{fullSongDescription} finished playing.";
                case PlayType.Seeked:
                    string timePosition = position.HasValue ? TimeSpan.FromSeconds(position.Value).ToString(@"mm\:ss") : "a new position";
                    return $"Seeked to {timePosition} in {fullSongDescription}.";
                case PlayType.Skipped:
                    return $"Skipped: {fullSongDescription}.";
                case PlayType.Restarted:
                    return $"Restarted: {fullSongDescription}.";
                case PlayType.SeekRestarted: // Potentially more specific than just Restarted
                    return $"Restarted {fullSongDescription} from the beginning.";
                case PlayType.CustomRepeat: // Assuming this means looping/repeating the current song
                    return $"Looping: {fullSongDescription}.";
                case PlayType.Previous:
                    return $"Playing previous track: {fullSongDescription}."; // Assumes currentSong is now the previous track

                // Chat related (could be more specific if song context is relevant, otherwise generic)
                case PlayType.ChatSent:
                    return "Message sent.";
                case PlayType.ChatReceived:
                    return "New message received.";
                // Add more chat types as needed with user-friendly messages...
                // e.g., "Message deleted.", "Your message was pinned."

                // Playlist and Sharing
                case PlayType.AddToPlaylist: // Note: Your enum has 37 for both Add and Remove. I'll assume one for now.
                                             // If they are distinct, you'll need another enum member.
                    return $"Added {fullSongDescription} to playlist.";
                // case PlayType.RemoveFromPlaylist: // If you have a distinct enum value
                //    return $"Removed {fullSongDescription} from playlist.";
                case PlayType.ShareSong:
                    return $"Sharing {fullSongDescription}.";
                case PlayType.ReceiveShare:
                    return $"Received shared song: {fullSongDescription}.";

                case PlayType.LogEvent: // Generic, as "LogEvent" is internal-facing
                    return "An event was recorded.";

                default:
                    // Fallback for any PlayType not explicitly handled
                    // Capitalize the first letter of the enum name and add spaces before other capitals for readability
                    string formattedType = System.Text.RegularExpressions.Regex.Replace(type.ToString(), "([A-Z])", " $1").TrimStart();
                    return $"{formattedType} event for {fullSongDescription}.";
            }
        }

        // You could add more methods here for other types of user logs if needed
        // For example, for user login, settings changes, etc.
        // public static string GetUserLoginMessage(string userName) => $"Welcome back, {userName}!";
    }
}
