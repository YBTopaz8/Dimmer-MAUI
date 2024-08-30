namespace Dimmer.Utilities.TypeConverters;
public class EmptyStringToMessageConverter : IValueConverter // TODO: RENAME THIS
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        
        if (targetType == typeof(bool))
        {            
            return true;
        }
        else if (targetType == typeof(string))
        {
            if (vm.SynchronizedLyrics != null && vm.SynchronizedLyrics.Count > 0)
            {
                return vm.SynchronizedLyrics;
            }
            if (!string.IsNullOrEmpty(vm.TemporarilyPickedSong.UnSyncLyrics))
            {
                return vm.TemporarilyPickedSong.UnSyncLyrics;
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
