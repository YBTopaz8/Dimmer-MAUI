namespace Dimmer_MAUI.Utilities.TypeConverters;
public class EmptyStringToMessageConverter : IValueConverter // TODO: RENAME THIS
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {        

        if (targetType == typeof(bool) && (string)parameter == "Sync")
        {
            var val = value as ObservableCollection<LyricPhraseModel>;
            if (val?.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
       
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
