namespace Dimmer.DimmerLive.Interfaces.Implementations;


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

    public async Task AutoLoginAsync()
    {
        if (ParseClient.Instance is null) return;
        if (CurrentUserValue == null || CurrentUserValue.SessionToken is null)
        {
            //get saved sessionToken

            var Tokenn = await SecureStorage.Default.GetAsync("userToken");

            if (Tokenn != null)
            {
                 await ParseClient.Instance.BecomeAsync(Tokenn);
                
            }

        }

        if (ParseClient.Instance.CurrentUser != null && !string.IsNullOrEmpty(ParseClient.Instance.CurrentUser.SessionToken))
        {
            var result = await InitializeAsync();
            if (result)
            {

            }
        }
    }
    // method for auto login if token exists
    // method to save to token to secure storage if remember me is checked
    public async Task SaveTokenAsync()
    {
        if ( ParseClient.Instance.CurrentUser != null)
        {
            await SecureStorage.Default.SetAsync("userToken", ParseClient.Instance.CurrentUser.SessionToken);
        }
        else
        {
            SecureStorage.Default.Remove("userToken");
        }
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
                await SaveTokenAsync();
                
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
        if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return new AuthResult(false, "Empty Params");
        }    
        try
        {
            var parseUser = await ParseClient.Instance.LogInWithAsync(username, password);
            await SaveTokenAsync();
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
            _currentUserSubject.OnNext(new UserModelOnline(ParseClient.Instance.CurrentUser));
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
