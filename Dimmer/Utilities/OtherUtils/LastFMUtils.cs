namespace Dimmer_MAUI.Utilities.OtherUtils;
public class LastFMUtils
{

    public static void ScrobbleTrack(Scrobble Scr)
    {
        if (LastfmClient.Instance.Session.Authenticated)
        {
            _ = LastfmClient.Instance.Track.ScrobbleAsync(Scr);
        }
    }

    public static void SetNowListeningOn(SongModelView Song)
    {
        if (LastfmClient.Instance.Session.Authenticated)
        {
            _ = LastfmClient.Instance.Track.UpdateNowPlayingAsync(Song.Title, Song.ArtistName);
        }
    }

    public static void LoveTrack(SongModelView Song)
    {
        GeneralStaticUtilities.RunFireAndForget(LastfmClient.Instance.Track.LoveAsync(Song.Title, Song.ArtistName), ex =>
        {
            // Log or handle the exception as needed
            Debug.WriteLine($"Task error: {ex.Message}");
        });

    }
    public static void UnLoveTrack(SongModelView Song)
    {
        GeneralStaticUtilities.RunFireAndForget(LastfmClient.Instance.Track.UnloveAsync(Song.Title, Song.ArtistName), ex =>
        {
            // Log or handle the exception as needed
            Debug.WriteLine($"Task error: {ex.Message}");
        });
    }
    
    public static void SetNowListening(SongModelView Song)
    {
        if (LastfmClient.Instance.Session.Authenticated)
        {
            GeneralStaticUtilities.RunFireAndForget(LastfmClient.Instance.Track.UpdateNowPlayingAsync(Song.Title, Song.ArtistName), ex =>
            {
                // Log or handle the exception as needed
                Debug.WriteLine($"Task error: {ex.Message}");
            });
        }
    }

    public static void RateSong(SongModelView Song, bool isLove)
    {
        //if (isLove)
        //{
        //    LoveTrack(Song);
        //}
        //else
        //{
        //    UnLoveTrack(Song);
        //}

    }

    public static async Task<bool> LogInToLastFMWebsite(string lastFMUname, string lastFMPass, bool isSilent=true)
    {
        return false;
        var clientLastFM = LastfmClient.Instance;
        if (clientLastFM.Session.Authenticated)
        {
            return await GeneralLastFMLogin(lastFMUname, lastFMPass, isSilent);
        }
        return false;
    }

    private static async Task<bool> GeneralLastFMLogin(string? lastFMUname, string? lastFMPass, bool isSilent)
    {
        return false;
        var clientLastFM = LastfmClient.Instance;
        //LoginBtn.IsEnabled = false;
        if (string.IsNullOrWhiteSpace(lastFMUname) || string.IsNullOrWhiteSpace(lastFMPass))
        {
            if (isSilent)
            {
                return false;
            }
            _ = Shell.Current.DisplayAlert("Error when logging to lastfm", "Username and Password are required.", "OK");
            return false;
        }

        //await clientLastFM.AuthenticateAsync(lastFMUname, lastFMPass);
        if (clientLastFM.Session.Authenticated && !isSilent)
        {
            await Shell.Current.DisplayAlert(lastFMUname, "Welcome Back !", "OK" );

            var usr = await clientLastFM.User.GetInfoAsync(lastFMUname);
            _ = SecureStorage.Default.SetAsync("LastFMUsername", usr.Name);
            _ = SecureStorage.Default.SetAsync("LastFMPassWord", lastFMPass);
            return true;
        }
        return false;
    }


    public static async Task<bool> QuickLoginToLastFM()
    {
        return false;
        var lastFMUname = await SecureStorage.Default.GetAsync("LastFMUsername");
        var lastFMPass = await SecureStorage.Default.GetAsync("LastFMPassWord");

        return await GeneralLastFMLogin(lastFMUname, lastFMPass, true);        
    }
}
