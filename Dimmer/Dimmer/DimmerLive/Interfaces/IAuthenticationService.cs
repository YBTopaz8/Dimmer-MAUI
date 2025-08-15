using Dimmer.DimmerLive.Interfaces.Services;

namespace Dimmer.DimmerLive.Interfaces;

public interface IAuthenticationService
{
    bool IsLoggedIn { get; }
    IObservable<UserModelOnline?> CurrentUser { get; }
    UserModelOnline? CurrentUserValue { get; }

    Task<bool> InitializeAsync();
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RegisterAsync(string username, string email, string password);
}