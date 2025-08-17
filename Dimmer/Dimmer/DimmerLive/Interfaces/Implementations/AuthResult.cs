namespace Dimmer.DimmerLive.Interfaces.Implementations;


/// <summary>
/// Represents the outcome of an authentication operation (login or registration).
/// </summary>
public record AuthResult(bool IsSuccess, string? ErrorMessage = null)
{
    /// <summary>
    /// </summary>
    public static AuthResult Success() => new(true);

    /// <summary>
    /// </summary>
    public static AuthResult Failure(string errorMessage) => new(false, errorMessage);
}