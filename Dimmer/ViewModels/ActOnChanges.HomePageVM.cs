using System.Threading.Tasks;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
    public bool IsControlKeyPressed { get; set; }
    public bool IsShiftKeyPressed { get; set; }
    
    partial void OnAllAlbumsChanging(ObservableCollection<AlbumModelView>? oldValue, ObservableCollection<AlbumModelView>? newValue)
    {
        //Debug.WriteLine($"Old alb {oldValue?.Count} new {newValue?.Count}");
    }
    partial void OnSynchronizedLyricsChanging(ObservableCollection<LyricPhraseModel> oldValue, ObservableCollection<LyricPhraseModel> newValue)
    {
        try
        {
            if (newValue is not null && newValue.Count < 1)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (SyncLyricsCV is not null)
                    {
                        SyncLyricsCV!.ItemsSource = null;
                    }

                });
            }
            if (newValue is not null && newValue.Count > 0)
            {

                //TODO MOBILE
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (SyncLyricsCV is not null)
                    {
                        SyncLyricsCV!.ItemsSource = null;
                        SyncLyricsCV.ItemsSource = newValue;

                        if (SyncLyricsCV is not null && CurrentAppState == AppState.OnForeGround)
                        {
                            SyncLyricsCV.ScrollTo(index: 0, position: ScrollToPosition.Start, animate: true);
                        }
                    }

                });
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message + "A");
        }
    }
    partial void OnDisplayedSongsChanged(ObservableCollection<SongModelView> oldValue, ObservableCollection<SongModelView> newValue)
    {
        if (newValue is not null && newValue.Count > 0)
        {

        }
    }

    partial void OnIsMultiSelectOnChanging(bool oldValue, bool newValue)
    {
        //throw new NotImplementedException();
    }
    partial void OnIsMultiSelectOnChanged(bool oldValue, bool newValue)
    {
        //throw new NotImplementedException()

        if (CurrentPage == PageEnum.MainPage)
        {
            switch (newValue)
            {
                case true:
                    DisplayedSongsColView.SelectionMode = SelectionMode.Multiple;
                    DisplayedSongsColView.SelectionChanged += DisplayedSongsColView_SelectionChanged;
                    break;
                case false:
                    DisplayedSongsColView.SelectionMode = SelectionMode.Single;
                    DisplayedSongsColView.SelectionChanged -= DisplayedSongsColView_SelectionChanged;
                    MultiSelectSongs.Clear();
                    break;
            }
        }


    }

    partial void OnDisplayedSongsChanging(ObservableCollection<SongModelView> oldValue, ObservableCollection<SongModelView> newValue)
    {
        Debug.WriteLine($"Old {oldValue?.Count} | New {newValue?.Count}");
    }


    partial void OnCurrentUserOnlineChanged(ParseUser? oldValue, ParseUser? newValue)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await ConnectToLiveQueriesAsync();
        });

        if (newValue is not null)
        {
            SongsMgtService.UpdateUserLoginDetails(newValue);
            IsLoggedIn = true;
        }
        else
        {
            IsLoggedIn = false; //do more here 
        }
    }

}
