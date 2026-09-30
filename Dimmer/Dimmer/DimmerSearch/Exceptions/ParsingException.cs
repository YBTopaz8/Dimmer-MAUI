namespace Dimmer.DimmerSearch.Exceptions;

public readonly record struct ParseResult<T>(T? Value, string? Error, int ErrorPosition, bool IsSuccess)
{
    public static ParseResult<T> Ok(T value) => new(value, null, -1, true);

  
    public static ParseResult<T> Fail(string? error, int position) => new(default, error, position, false);
}