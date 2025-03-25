namespace Dimmer_MAUI.Utilities.TypeConverters;
public class BoolToInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        HomePageVM? MyViewModel = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        switch (parameter)
        {
            
            case "playbtn":                
                if (!MyViewModel.TemporarilyPickedSong.IsPlaying)
                {
                    return false;
                }
                else
                {
                    return false;
                }
            case "pausebtn":
                if (!MyViewModel.TemporarilyPickedSong.IsPlaying)
                {
                    return true;
                }
                else
                {
                    return true;
                }
            default:
                bool val = !(bool)value;
                
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
        bool val = (bool)value;
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
