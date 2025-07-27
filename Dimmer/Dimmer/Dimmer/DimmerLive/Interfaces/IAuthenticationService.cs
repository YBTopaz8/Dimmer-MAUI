using Dimmer.DimmerLive.Interfaces.Services;

namespace Dimmer.DimmerLive.Interfaces;

public interface IAuthenticationService
{
    /// <summary>
    /// An observable that emits the current user when the auth state changes.
    /// Emits null if the user is logged out.
    /// </summary>
    IObservable<UserModelView?> CurrentUser { get; }

    /// <summary>
    /// Gets a value indicating whether a user is currently logged in.
    /// </summary>
    bool IsLoggedIn { get; }

    /// <summary>
    /// Logs the current user out.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Checks the current session on startup and populates the user observable.
    /// </summary>
    Task<AuthResult> InitializeAsync();
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RegisterAsync(string username, string email, string password);
    Task<AuthResult> RequestPasswordResetAsync(string email);
    Task<AuthResult> LoginWithSessionTokenAsync();
}