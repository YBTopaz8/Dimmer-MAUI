namespace Dimmer.DimmerSearch.Exceptions;
public class ParsingException : Exception
{
    public int Position { get; }

    public ParsingException(string message, int position = -1) : base(message)
    {
        Position = position;
    }
}