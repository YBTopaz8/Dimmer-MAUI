#if ANDROID
using AndroidX.RecyclerView.Widget;
#endif

namespace Dimmer_MAUI.Platforms.Android;
public static class PlatSpecificUtils
{
    public static bool DeleteSongFile(SongModelView song)
    {
        try
        {
            if (File.Exists(song.FilePath))
            {
                File.Delete(song.FilePath);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred: " + ex.Message);
            return false;
        }
    }
    public static bool MultiDeleteSongFiles(ObservableCollection<SongModelView> songs)
    {
        try
        {
            
            foreach (var song in songs)
            {
                if (File.Exists(song.FilePath))
                {
                    File.Delete(song.FilePath);                    
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred: " + ex.Message);
            return false;
        }
    }

    public static void ToggleWindowAlwaysOnTop(bool bof, nint nativeWindowHandle = 0)
    {
        Debug.WriteLine("Nothing"); ;
    }

    public static bool IsItemVisible (this CollectionView colView, object item)
    {
        var platformCollectionView = colView.Handler.PlatformView as RecyclerView;

        if (platformCollectionView == null)
            return false;

        var layoutManager = platformCollectionView.GetLayoutManager() as LinearLayoutManager;
        if (layoutManager == null)
            return false;

        var index = (colView.ItemsSource as System.Collections.IList).IndexOf(item);

        // Check if the item is within the visible range
        int firstVisibleItemPosition = layoutManager.FindFirstVisibleItemPosition();
        int lastVisibleItemPosition = layoutManager.FindLastVisibleItemPosition();

        return index >= firstVisibleItemPosition && index <= lastVisibleItemPosition;
    }
}