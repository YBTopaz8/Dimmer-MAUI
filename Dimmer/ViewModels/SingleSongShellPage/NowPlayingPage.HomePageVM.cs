using CommunityToolkit.Maui.Extensions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM
{
    [ObservableProperty]
    public partial int NPLyricsFontSize { get; set; }
    [ObservableProperty]
    public partial Label? InternalNotificationLabelVM { get; set; }
    [ObservableProperty]
    public partial SearchBar? InternalSearchSongSBVM { get; set; }
    [RelayCommand]
    public void IncreaseNowPlayingLyricsFontSize()
    {
        NPLyricsFontSize += 2;
    }
    [RelayCommand]
    public void DecreaseNowPlayingLyricsFontSize()
    {
        NPLyricsFontSize -= 2;
    }

    [RelayCommand]
    async Task SaveLyricsToLrcAfterSyncing()
    {
        bool result = await Shell.Current.DisplayAlert("Information","Done Syncing?", "Yes", "No");
        
        if (result)
        {
            string? lyr = string.Join(Environment.NewLine, LyricsLines!.Select(line => $"{line.TimeStampText} {line.Text}"));
            if (lyr is not null)
            {
                if (LyricsManagerService.WriteLyricsToLyricsFile(lyr, TemporarilyPickedSong!, true))
                {
                    await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
                    CurrentViewIndex = 0;
                }
                LyricsManagerService.InitializeLyrics(lyr);
                SongsMgtService.AllSongs.FirstOrDefault(x => x.LocalDeviceId == TemporarilyPickedSong!.LocalDeviceId)!.HasLyrics = true;
                GeneralStaticUtilities.ShowNotificationInternally("Saved Synced Lyrics To Device");
            }
        }
    }
    [ObservableProperty]
    public partial bool IsFetching { get; set; } = false;
    public async Task<bool> FetchLyrics(bool fromUI = false)
    {

        if (MySelectedSong is null || TemporarilyPickedSong is null || string.IsNullOrEmpty(TemporarilyPickedSong.FilePath))
        {
            return false;
        }
        LyricsSearchSongTitle = string.IsNullOrEmpty(LyricsSearchSongTitle) ? MySelectedSong.Title : LyricsSearchSongTitle;
        LyricsSearchArtistName= string.IsNullOrEmpty(LyricsSearchSongTitle) ? MySelectedSong.ArtistName : LyricsSearchArtistName;
        LyricsSearchAlbumName = string.IsNullOrEmpty(LyricsSearchSongTitle) ? MySelectedSong.AlbumName : LyricsSearchAlbumName;
        

        List<string> manualSearchFields =
        [
            LyricsSearchAlbumName,
            LyricsSearchArtistName,
            LyricsSearchSongTitle,
        ];

        (MySelectedSong.HasSyncedLyrics, MySelectedSong.SyncLyrics) = LyricsService.HasLyrics(MySelectedSong);
        if (MySelectedSong.HasSyncedLyrics)
        {
            IsFetchSuccessful = true;

        }

        //if (fromUI || SynchronizedLyrics?.Count < 1)
        //{
        AllSyncLyrics = [];
        (bool IsSuccessful, Content[]? contentData) = await LyricsManagerService.FetchLyricsOnlineLrcLib(MySelectedSong, true, manualSearchFields);

        AllSyncLyrics = contentData.ToObservableCollection();
        //(IsFetchSuccessful, var e) = await LyricsManagerService.FetchLyricsOnlineLyrist(MySelectedSong.Title, TemporarilyPickedSong.ArtistName);
        //if (e is not null)
        //{
            //AllSyncLyrics.Add(e.FirstOrDefault());
        //}


        IsFetchSuccessful = IsSuccessful;

        //LyricsSearchSongTitle = null;
        //LyricsSearchArtistName = null;
        //LyricsSearchAlbumName = null;

        return IsFetchSuccessful;
    }
    [ObservableProperty]
    public partial List<string>? LinkToFetchSongCoverImage { get; set; } = new();

    public async Task ShowSingleLyricsPreviewPopup(Content cont, bool IsPlain)
    {
        if (IsPlain)
        {
            MySelectedSong.UnSyncLyrics = cont.PlainLyrics;
            
            SongsMgtService.UpdateSongDetails(MySelectedSong);
            return;
        }
        var result = (bool)await Shell.Current.ShowPopupAsync(new SingleLyricsPreviewPopUp(cont!, IsPlain, this));
        if (result)
        {
            await SaveSelectedLyricsToFile(!IsPlain, cont);
            if (TemporarilyPickedSong is null)
                TemporarilyPickedSong = MySelectedSong;
        }
    }

    public async Task SaveSelectedLyricsToFile(bool isSync, Content cont) // rework this!
    {
        bool isSavedSuccessfully;

        if (MySelectedSong is null)
        {
            return;
        }
        if (!isSync)
        {
            MySelectedSong!.HasLyrics = true;
            MySelectedSong.UnSyncLyrics = cont.PlainLyrics;

            isSavedSuccessfully = LyricsManagerService.WriteLyricsToLyricsFile(cont.PlainLyrics!, MySelectedSong, isSync);
        }
        else
        {
            MySelectedSong.HasLyrics = false;

            MySelectedSong.HasSyncedLyrics = true;
            isSavedSuccessfully = LyricsManagerService.WriteLyricsToLyricsFile(cont.SyncedLyrics!, MySelectedSong, isSync);
        }
        if (isSavedSuccessfully)
        {
            await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
            AllSyncLyrics = [];
            CurrentViewIndex = 0;
        }
        else
        {
            await Shell.Current.DisplayAlert("Error !", "Failed to Save Lyrics!", "OK");
            return;
        }
        if (!isSync)
        {
            return;
        }
        LyricsManagerService.InitializeLyrics(cont.SyncedLyrics!);
        if (DisplayedSongs!.FirstOrDefault(x => x.LocalDeviceId == MySelectedSong.LocalDeviceId) is not null)
        {
            DisplayedSongs!.FirstOrDefault(x => x.LocalDeviceId == MySelectedSong.LocalDeviceId)!.HasLyrics = true;
        }
        //if (PlayBackService.CurrentQueue != 2)
        //{
        //    SongsMgtService.UpdateSongDetails(MySelectedSong);
        //}

    }

    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModel>? LyricsLines { get; set; } = new();
    [RelayCommand]
    async Task CaptureTimestamp(LyricPhraseModel lyricPhraseModel)
    {
        var CurrPosition = CurrentPositionInSeconds;
        if (!IsPlaying)
        {
            await Shell.Current.DisplayAlert("Warning", "You must be playing a song to capture a timestamp.", "OK");
            return;
            //PlaySong(TemporarilyPickedSong);
        }

        if (CurrPosition < 0)
        {
            return;
        }
        LyricPhraseModel? Lyricline = LyricsLines?.FirstOrDefault(x => x == lyricPhraseModel);
        if (Lyricline is null)
            return;


        Lyricline.TimeStampMs = (int)CurrPosition * 1000;
        Lyricline.TimeStampText = string.Format("[{0:mm\\:ss\\.ff}]", TimeSpan.FromSeconds(CurrPosition));

    }

    [RelayCommand]
    void DeleteLyricLine(LyricPhraseModel lyricPhraseModel)
    {
        LyricsLines?.Remove(lyricPhraseModel);
        if (TemporarilyPickedSong.UnSyncLyrics is null)
        {
            return;
        }
        TemporarilyPickedSong.UnSyncLyrics = RemoveTextAndFollowingNewline(TemporarilyPickedSong.UnSyncLyrics, lyricPhraseModel.Text);//TemporarilyPickedSong.UnSyncLyrics.Replace(lyricPhraseModel.Text, string.Empty);
    }

    string[]? splittedLyricsLines;

    public void PrepareLyricsSync(string? plainLyrics=null)
    {

        if (plainLyrics == null)
            return;



        // Define the terms to be removed using a regular expression
        string termsToRemovePattern = @"\[?\s*(Chorus(es)?|Verse(s)?|Hook(s)?|Bridge(s)?|Intro|Outro|Pre[- ]?Chorus|Instrumental|Interlude)\s*\]?";        //string termsToRemovePattern = string.Join("|", termsToRemove.Select(Regex.Escape)); // Alternative with your array.

        // Remove all the terms from the lyrics using Regex.Replace
        string cleanedLyrics = Regex.Replace(plainLyrics, termsToRemovePattern, "", RegexOptions.IgnoreCase);

        // Split and filter the lyrics lines
        string[]? ss = cleanedLyrics.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        splittedLyricsLines = ss?.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

        if (splittedLyricsLines is null || splittedLyricsLines.Length < 1)
        {
            return;
        }
        // Clear the existing items. This will trigger a UI update.
        LyricsLines?.Clear();

        // Add the new items.  This will trigger *another* UI update.
        foreach (var item in splittedLyricsLines)
        {
            LyricsLines.Add(new LyricPhraseModel(new LyricsPhrase(0, item)));
        }
    }
    static string RemoveTextAndFollowingNewline(string input, string textt)
    {
        // Escape the text to be removed to handle special regex characters.
        string escapedText = Regex.Escape(textt);
        // The regex:  Find the text, followed by an optional \r, then a required \n.
        //             Or, just the text at the end of the string.
        string pattern = $@"{escapedText}(\r?\n|$)";
        return Regex.Replace(input, pattern, "", RegexOptions.None); // or RegexOptions.Compiled for even more speed
    }

    [RelayCommand]
    public async Task FetchSongCoverImage(SongModelView? song = null)
    {
        if (song is null && MySelectedSong is not null)
        {
            if (!string.IsNullOrEmpty(MySelectedSong.CoverImagePath))
            {
                if (!File.Exists(MySelectedSong.CoverImagePath))
                {
                    MySelectedSong.CoverImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong.Title!, TemporarilyPickedSong.ArtistName!, TemporarilyPickedSong.AlbumName!, TemporarilyPickedSong);
                }
            }
            return;
        }
        else
        {
            var str = await LyricsManagerService.FetchAndDownloadCoverImage(song.Title!, song.ArtistName!, song.AlbumName!, song);
            MySelectedSong.CoverImagePath = str;
        }

    }

    [RelayCommand]
    public async Task FetchAlbumCoverImage(AlbumModelView album)
    {
        var firstSong = DisplayedSongs.Where(x => x.LocalDeviceId == album.LocalDeviceId).FirstOrDefault();
        if (album is not null)
        {

            if (!string.IsNullOrEmpty(album.AlbumImagePath))
            {
                if (!File.Exists(album.AlbumImagePath))
                {
                    album.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(firstSong.Title, firstSong.ArtistName, firstSong.AlbumName, firstSong);
                }
               
            }
            return;
        }
        else
        {
            AllAlbums.FirstOrDefault(x => x.LocalDeviceId == album.LocalDeviceId).AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(firstSong.Title, firstSong.ArtistName, firstSong.AlbumName, firstSong);
        }

    }
    System.Timers.Timer _showAndHideFavGif;
    
    public void RateSong(string value)
    {
        if (MySelectedSong is null || TemporarilyPickedSong is null || string.IsNullOrEmpty(TemporarilyPickedSong.FilePath))
        {
            return;
        }
        bool willBeFav = false;
        var rateValue = int.Parse(value);
        switch (rateValue)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                willBeFav = false;
                break;
            case 4:
            case 5:
                willBeFav = true;
                break;
            default:
                break;
        }
        MySelectedSong.IsFavorite = willBeFav;
        MySelectedSong.Rating = rateValue;
        SongsMgtService.UpdateSongDetails(MySelectedSong);

        var favPlaylist =  PlayBackService.AllPlaylists.Where(x=>x.Name == "Favorites").FirstOrDefault();
        if (favPlaylist == null)
        {
            return;
        }

            if (MySelectedSong.IsFavorite && willBeFav)
            {
                UpSertPlayList(MySelectedSong, IsAddSong: true, playlistModel: favPlaylist);
                GeneralStaticUtilities.ShowNotificationInternally("Added to Favorites");
                return;
            }
            if (MySelectedSong.IsFavorite && !willBeFav)
            {
                UpSertPlayList(MySelectedSong, IsRemoveSong: true, playlistModel: favPlaylist);
                GeneralStaticUtilities.ShowNotificationInternally("Removed from Favorites");

                return;
            }
        
        favPlaylist?.DisplayedSongsFromPlaylist?.Remove(MySelectedSong);
        if (SelectedPlaylist == favPlaylist)
        {
            SelectedPlaylist?.DisplayedSongsFromPlaylist?.Remove(MySelectedSong);
        }
        if (DisplayedPlaylists.Contains(favPlaylist))
        {
            SelectedPlaylist?.DisplayedSongsFromPlaylist?.Remove(MySelectedSong);
        }
        //if (CurrentUser.IsLoggedInLastFM)
        //{
        //    //LastFMUtils.RateSong(MySelectedSong, willBeFav);
        //}

    }
    [ObservableProperty]
    public partial TitleBar? DimmerTitleBarVM { get; set; } = new();
  
}
