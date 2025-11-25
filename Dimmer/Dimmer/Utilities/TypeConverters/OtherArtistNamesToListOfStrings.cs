namespace Dimmer.Utilities.TypeConverters;

public partial class OtherArtistNamesToListOfStringsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return null;


        if (value is not SongModelView song)
            return null;

        var val = song.OtherArtistsName;
        char[] dividers = [',', ';', ':', '|', '-'];

        var namesList = val
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) 
            .Select(name => name.Trim())                      
            .ToArray();                                             


        return namesList;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
