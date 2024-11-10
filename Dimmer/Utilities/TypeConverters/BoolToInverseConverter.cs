namespace Dimmer_MAUI.Utilities.TypeConverters;
public class BoolToInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        switch (parameter)
        {
            
            case "playbtn":                
                if (!vm.TemporarilyPickedSong.IsPlaying)
                {
                    return false;
                }
                else
                {
                    return false;
                }
            case "pausebtn":
                if (!vm.TemporarilyPickedSong.IsPlaying)
                {
                    return true;
                }
                else
                {
                    return true;
                }
            default:
                var val = !(bool)value;
                
                return val;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {

        Debug.WriteLine(message: "None");
        return null;
    }
}

public class BoolToYesNoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var val = (bool)value;
        if (val)
        {
            return "Yes";
        }
        else
        {
            return "No";
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.WriteLine(message: "None");
        return null;
    }
}
