using Dimmer.Data.Models;

using Hqub.Lastfm.Entities;

using Parse.Infrastructure;

using Realms;

namespace Dimmer.DimmerLive.Interfaces.Services;


public class ParseAuthenticationService : IAuthenticationService
{
    private readonly ILogger<ParseAuthenticationService> _logger;
    private readonly BehaviorSubject<UserModelOnline?> _currentUserSubject = new(null);

    public IObservable<UserModelOnline?> CurrentUser => _currentUserSubject.AsObservable();
    public UserModelOnline? CurrentUserValue => _currentUserSubject.Value;
    public bool IsLoggedIn => CurrentUserValue != null;

    public ParseAuthenticationService(ILogger<ParseAuthenticationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> InitializeAsync()
    {
        var parseUser = ParseUser.CurrentUser;
        if (parseUser != null)
        {
            try
            {
                await parseUser.FetchIfNeededAsync();
                _currentUserSubject.OnNext(new UserModelOnline(parseUser));
                _logger.LogInformation("User session restored for {Username}", parseUser.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid session token. Logging out.");
                await LogoutAsync();
                return false;
            }
        }
        else
        {
            _currentUserSubject.OnNext(null);
            _logger.LogInformation("No user session found. User is not logged in.");
            return false;
        }
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            var parseUser = await ParseClient.Instance.LogInWithAsync(username, password);
            _currentUserSubject.OnNext(new UserModelOnline(parseUser));
            return AuthResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Username}", username);
            return AuthResult.Failure(ex.Message);
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        var parseUser = new ParseUser { Username = username, Password = password, Email = email };
        try
        {
            await ParseClient.Instance.SignUpWithAsync(parseUser);
            _currentUserSubject.OnNext(new UserModelOnline(parseUser));
            return AuthResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Username}", username);
            return AuthResult.Failure(ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        _logger.LogInformation("Logging out user: {Username}", CurrentUserValue?.Username ?? "N/A");
        await ParseClient.Instance.LogOutAsync();
        _currentUserSubject.OnNext(null);
    }
}
