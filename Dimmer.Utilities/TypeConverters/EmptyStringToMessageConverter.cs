namespace Dimmer.Utilities.TypeConverters;
public class EmptyStringToMessageConverter : IValueConverter // TODO: RENAME THIS
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(bool))
        {
            if (value?.GetType() == typeof(ObservableCollection<LyricPhraseModel>))
            {
                var val = (ObservableCollection<LyricPhraseModel>)value;
                if (val.Count > 0 || val is null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if(value?.GetType() == typeof(SongsModelView))
            {
                var val = (SongsModelView)value;
                if (val.HasSyncedLyrics)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;

        }
        else if (targetType == typeof(string))
        {
            if (value?.GetType() == typeof(SongsModelView))
            {
                var val = (SongsModelView)value;
                if (val.HasSyncedLyrics)
                {
                    return string.Empty;
                }
                else
                {
                    if (!string.IsNullOrEmpty(val.UnSyncLyrics))
                    {
                        return val.UnSyncLyrics;
                        ;
                    }
                    else
                    {
                        return "No Lyrics Found...";
                        //return val.UnSyncLyrics;
                    }
                }

                return "No Lyrics Found...";
            }

            return "No Lyrics Found";
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
