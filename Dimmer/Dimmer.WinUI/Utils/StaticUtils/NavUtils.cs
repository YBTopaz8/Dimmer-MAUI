
namespace Dimmer.WinUI.Utils.StaticUtils;
public class NavUtils
{
    public static async Task NavigateToSingleSongShell(SongModelView song)
    {
        await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
    }
}
