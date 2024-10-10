
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Platforms.Android;
public static class PlatSpecificUtils
{
    public static async Task<bool> DeleteSongFile(SongsModelView song)
    {
        if (File.Exists(song.FilePath))
        {
            bool result = await Shell.Current.DisplayAlert("Delete File", $"Are you sure you want to Delete Song: {song.Title} by {song.ArtistName}?", "Yes", "No");
            if (result is true)
            {
                File.Delete(song.FilePath);

                return true;
            }
        }
        return false;
    }
}
