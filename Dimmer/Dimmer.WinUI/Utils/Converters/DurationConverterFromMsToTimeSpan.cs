namespace Dimmer.WinUI.Utils.Converters
{
    public sealed partial class DurationConverterFromSecondsToTimeSpan : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double duration)
            {
                var time = TimeSpan.FromSeconds(duration);
                return time.ToString(@"mm\:ss");
            }
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string str && TimeSpan.TryParse(str, out var span))
                return span.TotalMilliseconds;
            return 0d;
        }
    }
}
