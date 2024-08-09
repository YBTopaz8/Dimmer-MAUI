namespace Dimmer.Utilities.TypeConverters;
public class EmptyStringToMessageConverter : IValueConverter // TODO: RENAME THIS
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value?.GetType() == typeof(ObservableCollection<LyricPhraseModel>))
        {
            var val = (ObservableCollection<LyricPhraseModel>)value;
            if (val.Count > 0)
                return true;

        }

        if (value == null)
        {
            return "No Lyrics Found";
        }
        if (value?.GetType() == typeof(SongsModelView))
        {
            var val = (SongsModelView)value;
            if (string.IsNullOrEmpty(val.UnSyncLyrics) && !val.HasSyncedLyrics)
            {
                return true;
            }
            else if(val.HasSyncedLyrics)
            {
                return false;
            }
        }
        if (value?.GetType() == typeof(string))
        {
            var val = (string)value;
            if (string.IsNullOrEmpty(val))
            {
                return "No Lyrics Found";
            }
            else
            {
                return value;
            }
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
