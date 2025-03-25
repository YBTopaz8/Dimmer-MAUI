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
            
            foreach (SongModelView song in songs)
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
        RecyclerView? platformCollectionView = colView.Handler.PlatformView as RecyclerView;

        if (platformCollectionView == null)
            return false;

        LinearLayoutManager? layoutManager = platformCollectionView.GetLayoutManager() as LinearLayoutManager;
        if (layoutManager == null)
            return false;

        int index = (colView.ItemsSource as System.Collections.IList).IndexOf(item);

        // Check if the item is within the visible range
        int firstVisibleItemPosition = layoutManager.FindFirstVisibleItemPosition();
        int lastVisibleItemPosition = layoutManager.FindLastVisibleItemPosition();

        return index >= firstVisibleItemPosition && index <= lastVisibleItemPosition;
    }
}