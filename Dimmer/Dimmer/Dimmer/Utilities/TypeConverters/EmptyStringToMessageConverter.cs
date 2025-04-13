
namespace Dimmer.Utilities.TypeConverters;
public class EmptyStringToMessageConverter : IValueConverter // TODO: RENAME THIS
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {        

        if (targetType == typeof(bool) && (string)parameter == "Sync")
        {
            ObservableCollection<LyricPhraseModelView>? val = value as ObservableCollection<LyricPhraseModelView>;
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
