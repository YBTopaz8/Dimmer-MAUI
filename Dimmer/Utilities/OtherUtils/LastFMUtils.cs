using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public static void LoveTrack(SongModelView Song)
    {
        _ = LastfmClient.Instance.Track.LoveAsync(Song.Title, Song.ArtistName);

    }
    public static void UnLoveTrack(SongModelView Song)
    {
        _ = LastfmClient.Instance.Track.UnloveAsync(Song.Title, Song.ArtistName);
    }


    public static void RateSong(SongModelView Song, bool isLove)
    {
        if (isLove)
        {
            LoveTrack(Song);
        }
        else
        {
            UnLoveTrack(Song);
        }

    }

    public static async Task LogInToLastFMWebsite(string? lastFMUname = null, string? lastFMPass = null)
    {
        if (string.IsNullOrEmpty(lastFMUname) || string.IsNullOrEmpty(lastFMPass))
        {
            lastFMUname = await SecureStorage.Default.GetAsync("LastFMUsername");
            lastFMPass = await SecureStorage.Default.GetAsync("LastFMPassWord");
        }

        if (string.IsNullOrEmpty(lastFMUname) || string.IsNullOrEmpty(lastFMPass))
        {
            
        }
        var clientLastFM = LastfmClient.Instance;
        if (!clientLastFM.Session.Authenticated)
        {
            //LoginBtn.IsEnabled = false;
            if (string.IsNullOrWhiteSpace(lastFMUname) || string.IsNullOrWhiteSpace(lastFMPass))
            {
                _ = Shell.Current.DisplayAlert("Error when logging to lastfm", "Username and Password are required.", "OK");
                return;
            }
            await clientLastFM.AuthenticateAsync(lastFMUname, lastFMPass);
            if (clientLastFM.Session.Authenticated)
            {
                await Shell.Current.DisplayAlert(lastFMUname, "Welcome Back !", "OK");
                
                var usr = await clientLastFM.User.GetInfoAsync(lastFMUname);
                _ = SecureStorage.Default.SetAsync("LastFMUsername", usr.Name);
                _ = SecureStorage.Default.SetAsync("LastFMPassWord", lastFMPass);
            }
        }


    }
}
