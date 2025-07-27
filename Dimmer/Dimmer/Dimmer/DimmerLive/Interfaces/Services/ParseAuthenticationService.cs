using Dimmer.Data.Models;

using Parse.Infrastructure;

using Realms;

namespace Dimmer.DimmerLive.Interfaces.Services;


public class ParseAuthenticationService : IAuthenticationService
{
    private readonly ILogger<ParseAuthenticationService> _logger;
    private readonly IRealmFactory _realmFactory;
    private readonly IMapper _mapper;

    // A BehaviorSubject holds the latest value for new subscribers. Perfect for auth state.
    private readonly BehaviorSubject<UserModelView?> _currentUserSubject = new(null);

    public IObservable<UserModelView?> CurrentUser => _currentUserSubject;
    public UserModelView? CurrentUserValue => _currentUserSubject.Value;
    public bool IsLoggedIn => _currentUserSubject.Value != null;

    public ParseAuthenticationService(ILogger<ParseAuthenticationService> logger, IRealmFactory realmFactory, IMapper mapper)
    {
        _logger = logger;
        _realmFactory = realmFactory;
        _mapper = mapper;
    }

    public async Task<AuthResult> InitializeAsync()
    {
        var parseUser = ParseClient.Instance.CurrentUser;


        if (parseUser == null)
        {
            var ee = await LoginWithSessionTokenAsync();

            if (!ee.IsSuccess)
            {

                await Shell.Current.DisplayAlert("No User Found", "No user is currently logged in. Please log in or register.", "OK");
                _logger.LogInformation("No user session found. Prompting for login.");
                _currentUserSubject.OnNext(null);
                return AuthResult.Failure("no");
            }

            parseUser = ParseClient.Instance.CurrentUser;

            if (parseUser != null)
            {
                try
                {
                    await parseUser.FetchIfNeededAsync();
                    _logger.LogInformation("Found existing user session for: {Username}", parseUser.Username);
                    var userModelView = await SyncUserToLocalDbAsync(parseUser);
                    _currentUserSubject.OnNext(userModelView);
                    return new AuthResult(true, "OK");
                
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch session user. Forcing logout.");
                    await LogoutAsync(); // Clear the invalid session
                    return new AuthResult(false, ex.Message);
                }
            
            }
            else
            {
                _logger.LogInformation("No active user session found.");
                _currentUserSubject.OnNext(null);
                return new AuthResult(false, "no");
            }
        }


        else
        {
            return AuthResult.Failure("Failed");
        }
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            var parseUser = await ParseClient.Instance.LogInWithAsync(username, password);
            _logger.LogInformation("User logged in successfully: {Username}", parseUser.Username);
            var userModelView = await SyncUserToLocalDbAsync(parseUser);
            _currentUserSubject.OnNext(userModelView);
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user: {Username}", username);
            return new AuthResult(false, ex.Message);
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        var parseUser = new ParseUser()
        {
            Username = username,
            Password = password,
            Email = email
        };

        try
        {
            await ParseClient.Instance.SignUpWithAsync(parseUser);
            _logger.LogInformation("User registered and logged in: {Username}", parseUser.Username);
            var userModelView = await SyncUserToLocalDbAsync(parseUser);
            _currentUserSubject.OnNext(userModelView);
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user: {Username}", username);
            return new AuthResult(false, ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        _logger.LogInformation("Logging out user: {Username}", _currentUserSubject.Value?.Username ?? "N/A");
        await ParseClient.Instance.LogOutAsync();
        _currentUserSubject.OnNext(null);
    }

    /// <summary>
    /// Creates or updates the user in the local Realm database to match the server state.
    /// This is a critical step for data consistency.
    /// </summary>
    private async Task<UserModelView> SyncUserToLocalDbAsync(ParseUser parseUser)
    {
        using var realm = _realmFactory.GetRealmInstance();
        UserModel? userModel = null;

        await realm.WriteAsync(() =>
        {
            // Try to find the user by their Parse ObjectId
            var usrs= realm.All<UserModel>().ToList();

            if(usrs is not null && usrs.Count > 0)
            {
                userModel = usrs.First();
                userModel.UserName=parseUser.Username;
                userModel.UserEmail = parseUser.Email;
                userModel.SessionToken=parseUser.SessionToken;
            }

            if (userModel == null)
            {
                _logger.LogInformation("Creating new user in local DB: {Username}", parseUser.Username);
                userModel = new UserModel
                {
                    Id=new ObjectId(),
                    UserIDOnline = parseUser.ObjectId,
                    UserName = parseUser.Username,
                    UserEmail = parseUser.Email,
                    UserDateCreated = parseUser.CreatedAt ?? DateTimeOffset.UtcNow
                    ,
                    SessionToken=parseUser.SessionToken
                };
                realm.Add(userModel, update: true);
            }
            else
            {
                _logger.LogInformation("Updating existing user in local DB: {Username}", parseUser.Username);
             
            }
        });

        // Map the managed Realm object to a ViewModel for the UI
        return _mapper.Map<UserModelView>(userModel);
    }
    public async Task<AuthResult> RequestPasswordResetAsync(string email)
    {
        try
        {
            await ParseClient.Instance.RequestPasswordResetAsync(email);
            _logger.LogInformation("Password reset request sent for email: {Email}", email);
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset request failed for email: {Email}", email);
            return new AuthResult(false, ex.Message);
        }
    }



    public async Task<AuthResult> LoginWithSessionTokenAsync()
    {
        string sessionToken=string.Empty;
        var realm = _realmFactory.GetRealmInstance();
        var usrs = realm.All<UserModel>().ToList();
        UserModel userModel = null;
        if (usrs is not null && usrs.Count > 0)
        {
            userModel = usrs.First();
            sessionToken = userModel.SessionToken;
        }
        if (string.IsNullOrEmpty(sessionToken))
        {
            return AuthResult.Failure("Session token cannot be null or empty.");
        }

        try
        {
            // This is the key Parse SDK method. It validates the token with the server
            // and returns the full user object if the session is still valid.
            var parseUser = await ParseClient.Instance.BecomeAsync(sessionToken);

            _logger.LogInformation("User session restored successfully: {Username}", parseUser.Username);
            var userModelView = await SyncUserToLocalDbAsync(parseUser);
            _currentUserSubject.OnNext(userModelView);
            return AuthResult.Success();
        }
        catch (ParseFailureException ex) when (ex.Code == ParseFailureException.ErrorCode.InvalidSessionToken)
        {
            _logger.LogWarning("Invalid or expired session token provided. Forcing logout.");
            await LogoutAsync(); // The token is bad, so clear any local state.
            return AuthResult.Failure("Your session has expired. Please log in again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log in with session token.");
            return AuthResult.Failure("An unexpected error occurred while restoring your session.");
        }
    }
}

