namespace Dimmer.WinUI.Utils.Converters;

public partial class VolumeToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
            return "0";

        return (int)Math.Clamp(((double)(value) * 100), 0, 100) + "%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
