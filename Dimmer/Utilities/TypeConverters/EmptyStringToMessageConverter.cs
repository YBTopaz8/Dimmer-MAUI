namespace Dimmer_MAUI.Utilities.TypeConverters;
public class EmptyStringToMessageConverter : IValueConverter // TODO: RENAME THIS
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        

        if (targetType == typeof(bool) && (string)parameter == "Sync")
        {
            var val = value as ObservableCollection<LyricPhraseModel>;
            if (val.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (targetType == typeof(bool) && (string)parameter == "UnSync")
        {
            var val = value as string;
            if (!string.IsNullOrEmpty(val))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //else if (targetType == typeof(string) && (string)parameter == "UnSync")
        //{
        //    var val = value as string;
        //    if (string.IsNullOrEmpty(val))
        //    {
        //        return
        //    }
        //}
        return string.Empty;   
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
