using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Utilities.OtherUtils;
public static class ParseStaticUtils
{

    public static async Task UpdateSongStatusOnline(SongModelView Song, bool isUserAuthenticated)
    {
        return;
        //later!
        if (!isUserAuthenticated)
        {
            return;
        }
        var query = ParseClient.Instance.GetQuery(nameof(SongModelView))
            .WhereEqualTo(nameof(Song.LocalDeviceId), Song.LocalDeviceId);
        var parseObjects = await query.FindAsync();
        var pObj = parseObjects.FirstOrDefault();
        if (pObj != null)
        {
            pObj[nameof(Song.IsPlaying)] = Song.IsPlaying;
            await pObj.SaveAsync();
        }
    }
}
