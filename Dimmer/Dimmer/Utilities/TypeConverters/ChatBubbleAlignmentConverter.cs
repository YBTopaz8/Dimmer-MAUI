namespace Dimmer.Utilities.TypeConverters;

public class ChatBubbleAlignmentConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value indicating if the message was sent by the current user
    /// into a LayoutOptions value for horizontal alignment.
    /// </summary>
    /// <param name="value">The boolean value. Expects 'true' if sent by me.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An unused parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>LayoutOptions.End for sent messages, LayoutOptions.StartAsync for received messages.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSentByMe)
        {
            
            return isSentByMe ? LayoutOptions.End : LayoutOptions.Start;
        }

        return LayoutOptions.Start; // Default to left alignment
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This is not needed for one-way bindings.
        throw new NotImplementedException();
    }
}