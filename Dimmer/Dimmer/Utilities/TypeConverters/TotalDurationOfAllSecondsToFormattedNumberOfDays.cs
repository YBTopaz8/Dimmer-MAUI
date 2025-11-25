namespace Dimmer.Utilities.TypeConverters;
public class TotalDurationOfAllSecondsToFormattedNumberOfDays : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {

        //take all songs in lists, gets their durations in seconds, adds them up, and converts to days to show "list duration: X days"
        if (value is double totalSeconds)
        {
            var totalDays = totalSeconds / 86400; // 86400 seconds in a day
            return $"{totalDays:N2} days";
        }
        return null; // Or you could return Binding.DoNothing;


    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Compare this snippet from Dimmer/Dimmer/Utilities/TypeConverters/TotalDurationOfAllSecondsToFormattedNumberOfHours.cs:
public class TotalDurationOfAllSecondsToFormattedNumberOfHours : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        //take all songs in lists, gets their durations in seconds, adds them up, and converts to hours to show "list duration: X hours"
        if (value is double totalSeconds)
        {
            var totalHours = totalSeconds / 3600; // 3600 seconds in an hour
            return $"{totalHours:N2} hours";
        }
        return null; // Or you could return Binding.DoNothing;
        }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
        throw new NotImplementedException();
    }
    }
// Compare this snippet from Dimmer/Dimmer/Utilities/TypeConverters/TotalDurationOfAllSecondsToFormattedNumberOfMinutes.cs:
public class TotalDurationOfAllSecondsToFormattedNumberOfMinutes : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        //take all songs in lists, gets their durations in seconds, adds them up, and converts to minutes to show "list duration: X minutes"
        if (value is double totalSeconds)
        {
            var totalMinutes = totalSeconds / 60; // 60 seconds in a minute
            return $"{totalMinutes:N2} minutes";
        }
        return null; // Or you could return Binding.DoNothing;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
// Compare this snippet from Dimmer/Dimmer/Utilities/TypeConverters/TotalDurationOfAllSecondsToFormattedNumberOfSeconds.cs:
public class TotalDurationOfAllSecondsToFormattedNumberOfSeconds : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        //take all songs in lists, gets their durations in seconds, adds them up, and shows "list duration: X seconds"
        if (value is double totalSeconds)
        {
            return $"{totalSeconds:N2} seconds";
        }
        return null; // Or you could return Binding.DoNothing;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
