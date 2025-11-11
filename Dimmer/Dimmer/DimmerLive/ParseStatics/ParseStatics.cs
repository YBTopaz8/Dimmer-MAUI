using Parse.Infrastructure;

using System.Linq.Dynamic.Core.Exceptions;

namespace Dimmer.DimmerLive.ParseStatics;
public class ParseStatics
{

    public static class MessageTypes
    {
        public const string Text = "Text";
        public const string Image = "Image";
        public const string Video = "Video";
        public const string Audio = "Audio";
        public const string Location = "Location";
        public const string SongShare = "SongShare";
        public const string System = "System"; // For "User A joined", etc.
    }
    public static async Task<AppUpdateModel?> CheckForAppUpdatesAsync()
    {
        try
        {

            var updates = await ParseClient.Instance.CallCloudCodeFunctionAsync<IList<AppUpdateModel>>("getLatestAppUpdate", new Dictionary<string, object>());

            // Use LINQ's FirstOrDefault to safely get the first item, or null if the list is empty.
            var latestUpdate = updates.FirstOrDefault();

            if (latestUpdate != null)
            {
                var currVer = BaseViewModel.CurrentAppVersion;
                var currStage = BaseViewModel.CurrentAppStage;
                if (currVer == latestUpdate.appVersion && currStage==latestUpdate.appStage)
                {
                    return null;
                }
                string version = latestUpdate.Get<string>("title");
                Debug.WriteLine($"Latest update found: {version}");
                // Compare with your app's current version and show a notification if needed.
            }
            else
            {
                Debug.WriteLine("No app updates found. You are up to date.");
            }
            return latestUpdate;
        }
        catch (ParseFailureException e)
        {
            Debug.WriteLine($"Could not check for updates: {e.Message}");
            return null;
        }
    }
    public static async Task<int> GetChannelMessageCountAsync(string channelId)
    {
        try
        {
            var parameters = new Dictionary<string, object> { { "channelId", channelId } };
            var metadata = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("getChannelMetadata", parameters);

            return Convert.ToInt32(metadata["messageCount"]);
        }
        catch (ParseFailureException e)
        {
            Debug.WriteLine($"Error getting channel metadata: {e.Message}");
            return 0;
        }
    }
    public static async Task PostNewUpdateAsync(string title, string notes, string url)
    {
        try
        {
            var parameters = new Dictionary<string, object>
        {
            { "title", title },
            { "notes", notes },
            { "url", url }
        };
            await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("postAppUpdate", parameters);
            Debug.WriteLine("App update posted.");
        }
        catch (ParseFailureException e)
        {
            // Will throw "OPERATION_FORBIDDEN" if user is not an Admin
            Debug.WriteLine($"Failed to post update: {e.Message}");
        }
    }
    public static async Task<bool> SendContactFormAsync(string name, string email, string subject, string body)
    {
        try
        {
            var parameters = new Dictionary<string, object>
        {
            { "fromName", name },
            { "fromEmail", email },
            { "subject", subject },
            { "messageBody", body }
        };

            var response = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("sendContactEmail", parameters);

            Debug.WriteLine(response["message"]); // "Your message has been sent successfully."
                                                    // Show a success alert to the user
            return true;
        }
        catch (ParseException e)
        {
            Debug.WriteLine($"Error sending message: {e.Message}");
            // Show an error alert to the user
            return false;
        }
    }
    public static async Task SaveTqlQueryAsync(string queryName, string tqlString)
    {
        try
        {
            var parameters = new Dictionary<string, object>
        {
            { "name", queryName },
            { "tql", tqlString }
        };
            await ParseClient.Instance.CallCloudCodeFunctionAsync<ParseObject>("saveUserTqlQuery", parameters);
            Debug.WriteLine($"Query '{queryName}' saved successfully.");
        }
        catch (ParseFailureException e)
        {
            Debug.WriteLine($"Error saving TQL query: {e.Message}");
        }
    }
    public static async Task<IEnumerable<IDictionary<string, object>>?> FindMusicalNeighborsAsync()
    {
        if (ParseUser.CurrentUser == null)
            return null;

        try
        {
            // No parameters needed; the server knows who the current user is.
            var neighbors = await ParseClient.Instance.CallCloudCodeFunctionAsync<IEnumerable<IDictionary<string, object>>>("findMusicalNeighbors", new Dictionary<string, object>());

            foreach (var neighborData in neighbors)
            {
                var userPointer = (ParseUser)neighborData["user"];
                var overlapCount = Convert.ToInt32(neighborData["overlap"]);
                // You must fetch the user to get their username, etc.
                await userPointer.FetchIfNeededAsync();
                Debug.WriteLine($"Neighbor: {userPointer.Username}, Shared Artist Overlap: {overlapCount}");
            }
            return neighbors;
        }
        catch (ParseFailureException e)
        {
            Debug.WriteLine($"Error finding musical neighbors: {e.Message}");
            return null;
        }
    }
    public static async Task<ParseObject?> GetSharedSongDetailsAsync(string sharedSongId)
    {
        try
        {
            var parameters = new Dictionary<string, object> { { "sharedSongId", sharedSongId } };
            var sharedSong = await ParseClient.Instance.CallCloudCodeFunctionAsync<ParseObject>("getSharedSongDetails", parameters);

            Debug.WriteLine($"Fetched song details for: {sharedSong.Get<string>("Title")}");
            // Now you can use sharedSong.Get<ParseFile>("audioFile").Url to play the song.
            return sharedSong;
        }
        catch (ParseFailureException e)
        {
            Debug.WriteLine($"Error fetching shared song details: {e.Message}");
            return null;
        }
    }
    public static async Task<IEnumerable<ParseObject>?> GetMyTqlQueriesAsync()
    {
        try
        {
            var queries = await ParseClient.Instance.CallCloudCodeFunctionAsync<IEnumerable<ParseObject>>("getUserTqlQueries", new Dictionary<string, object>());
            Debug.WriteLine($"Fetched {queries.Count()} saved queries.");
            return queries;
        }
        catch (ParseFailureException e)
        {
            Debug.WriteLine($"Error fetching TQL queries: {e.Message}");
            return Enumerable.Empty<ParseObject>();
        }
    }
}
