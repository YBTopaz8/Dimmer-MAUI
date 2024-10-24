using Microsoft.VisualBasic.FileIO;
using Foundation= Windows.Foundation;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace Dimmer_MAUI.Platforms.Windows;
public static class PlatSpecificUtils
{
    public static bool DeleteSongFile(SongsModelView song)
    {
        try
        {
            if (File.Exists(song.FilePath))
            {
                    FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

            }
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
            return false;
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }

    public static async Task<bool> MultiDeleteSongFiles(ObservableCollection<SongsModelView> songs)
    {
        try
        {
            foreach (var song in songs)
            {
                if (File.Exists(song.FilePath))
                {
                    FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                }
            }
            
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
            return false;
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }
       
    // Method to set the window on top
    public static void ToggleWindowAlwaysOnTop(bool topMost, AppWindowPresenter appPresenter)
    {
        try
        {

            var OverLappedPres = appPresenter as OverlappedPresenter;
            if (topMost)
            {

                OverLappedPres!.IsAlwaysOnTop = true;
            }
            else
            {
                OverLappedPres!.IsAlwaysOnTop = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
        }
    }

}
