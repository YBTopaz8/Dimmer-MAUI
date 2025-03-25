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
        ParseQuery<ParseObject> query = ParseClient.Instance.GetQuery(nameof(SongModelView))
            .WhereEqualTo(nameof(Song.LocalDeviceId), Song.LocalDeviceId);
        IEnumerable<ParseObject> parseObjects = await query.FindAsync();
        ParseObject pObj = parseObjects.FirstOrDefault();
        if (pObj != null)
        {
            pObj[nameof(Song.IsPlaying)] = Song.IsPlaying;
            await pObj.SaveAsync();
        }
    }
}
