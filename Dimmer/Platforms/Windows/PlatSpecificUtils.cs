using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace Dimmer_MAUI.Platforms.Windows;
public static class PlatSpecificUtils
{
    public static async Task<bool> DeleteSongFile(SongsModelView song)
    {
        try
        {
            if (File.Exists(song.FilePath))
            {
                bool result = await Shell.Current.DisplayAlert("Delete Song", $"Are you sure you want to Delete " +
                    $"{song.Title} by {song.ArtistName} from your Device?", "Yes", "No");
                if (result is true)
                {
                    FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

                    return true;
                }
            }
            return false;
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
}
