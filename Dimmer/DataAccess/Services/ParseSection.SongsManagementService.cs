using Parse.Infrastructure;
using System.Threading.Tasks;

namespace Dimmer_MAUI.DataAccess.Services;
public partial class SongsManagementService
{

    #region Online Region

    public void UpdateUserLoginDetails(ParseUser usrr)
    {
        CurrentUserOnline = usrr;
        CurrentOfflineUser.UserEmail = usrr.Email;
        CurrentOfflineUser.UserName = usrr.Username;

        CurrentOfflineUser.LastSessionDate = DateTimeOffset.UtcNow;
        UserModel usr = new(CurrentOfflineUser);
        db = Realm.GetInstance(DataBaseService.GetRealm());
        db.Write(() =>
        {
            db.Add(usr, update: true);
        });
    }
    //delete
    public async Task<bool> LogUserOnlineAsync(string email, string password)
    {
        try
        {
            // Log the user in
            ParseUser e = await ParseClient.Instance.LogInWithAsync(email, password);

            // Check if the email is verified (if applicable)
            if (CurrentUserOnline is not null)
            {
                if (await CurrentUserOnline.IsAuthenticatedAsync())
                {
                    return true;
                }

            }
            ParseUser user = await ParseClient.Instance.GetCurrentUser();
            if (user.Get<bool>("emailVerified"))
            {
                Debug.WriteLine("Login successful. Email is verified.");
                return true;
            }
            else
            {
                Debug.WriteLine("Login successful, but email is not verified.");
                return false; // Deny further access until verification
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Login failed: {ex.Message}");
            return false; // Login failed
        }
    }
    public bool RequestPasswordResetAsync(string email)
    {
        try
        {
            _ = ParseClient.Instance.RequestPasswordResetAsync(email);
            return true; // Success: Reset email sent
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send password reset email: {ex.Message}");
            return false; // Failed: Handle error (e.g., invalid email)
        }
    }
    public void LogOutUser()
    {
        if (CurrentUserOnline is null)
        {
            return;
        }
        ParseClient.Instance.LogOut();

        Debug.WriteLine("User logged out successfully.");
    }
    public async Task<bool> IsEmailVerified()
    {
        // Check if the email is verified (if applicable)
        if (CurrentUserOnline is not null)
        {
            if (await CurrentUserOnline.IsAuthenticatedAsync())
            {
                return true;
            }

        }
        ParseUser user = await ParseClient.Instance.GetCurrentUser();

        if (user != null && user.Get<bool>("emailVerified"))
        {
            return true;
        }

        Debug.WriteLine("Email not verified.");
        return false;
    }
    #endregion
    public async Task<UserModelView?> GetUserAccountOnline()
    {
        try
        {
            CurrentOfflineUser ??= new ()
                {
                    UserName = string.Empty,
                    UserEmail = string.Empty,
                    UserPassword = string.Empty,
                };
            if (CurrentUserOnline is null)
            {
                ParseUser? oUser = await ParseClient.Instance.LogInWithAsync(CurrentOfflineUser.UserName, CurrentOfflineUser.UserPassword);

                if (oUser is null)
                {
                    return null;
                }
                CurrentUserOnline = oUser;
                MyViewModel.CurrentUserOnline = oUser;

                
            }

            if (await CurrentUserOnline.IsAuthenticatedAsync())
            {
                return null;
            }
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                UserModel? user = new()
                {
                    UserEmail = CurrentUserOnline.Email,
                    UserName = CurrentUserOnline.Username,
                    UserPassword = CurrentUserOnline.Password
                };


                IQueryable<UserModel> userdb = db.All<UserModel>();
                if (userdb.Any())
                {
                    UserModel usr = userdb.FirstOrDefault()!;
                    usr.UserEmail = user.UserEmail;
                    usr.UserName = user.UserName;
                    usr.UserPassword = user.UserPassword;

                    db.Add(usr, update: true);
                }

            });
            return null;

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error getting user account online: " + ex.Message);
            await Shell.Current.DisplayAlert("Hey1!", ex.Message, "Ok");
            return null;
        }
    }

    public async Task<bool> LoginAndCheckEmailVerificationAsync(string username, string password)
    {
        try
        {
            // Log the user in
            await ParseClient.Instance.LogInWithAsync(username, password);

            // Check if the email is verified
            ParseUser user = await ParseClient.Instance.GetCurrentUser();
            if (user.Get<bool>("emailVerified"))
            {
                Debug.WriteLine("Login successful and email verified!");
                return true; // User can proceed
            }
            else
            {
                // Re-send the verification email
                user.Email = user.Email; // This triggers the email resend
                await user.SaveAsync(); // Save the user to resend the verification email

                Debug.WriteLine("Email not verified. Verification email re-sent.");
                return false; // Block access until email is verified
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Login failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteUserAccountAsync()
    {
        try
        {

            // Check if the email is verified (if applicable)
            if (CurrentUserOnline is not null)
            {
                //ASK USER TO LOGIN first
                return false;

            }
            ParseUser user = await ParseClient.Instance.GetCurrentUser();

            if (user != null)
            {
                await user.DeleteAsync();
                ParseClient.Instance.LogOut(); // Log out after deletion
                Debug.WriteLine("User account deleted successfully.");
                return true;
            }

            Debug.WriteLine("No user is currently logged in.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to delete user account: {ex.Message}");
            return false;
        }
    }
    public static void ConnectOnline(bool ToLoginUI = true)
    {
        InitializeParseClient();

        return;
    }

    #region Region 2
    public async Task SendAllDataToServerAsInitialSync()
    {

        GetUserAccount();
        GetSongs();

        if (!CurrentOfflineUser.IsAuthenticated)
        {

            try
            {
                _ = await GetUserAccountOnline();
            }
            catch (Exception ex)
            {
                // Handle GetUserAccountOnline exceptions
                Debug.WriteLine($"Error in GetUserAccountOnline: {ex.Message}");
            }
        }
        try
        {

            //GeneralStaticUtilities.RunFireAndForget(AddSongToArtistWithArtistIDAndAlbumAndGenreOnlineAsync(AllArtists, AllAlbums, AllSongs, AllGenres, AllLinks, AllPlayDataLinks), ex =>
            //{
            //    // Log or handle the exception as needed
            //    Debug.WriteLine($"Task error: {ex.Message}");
            //});

        }
        catch (Exception ex)
        {
            // Handle GetAllDataFromOnlineAsync exceptions
            Debug.WriteLine($"Error in GetAllDataFromOnlineAsync: {ex.Message}");
        }

    }

    public UserModelView? GetUserAccount(ParseUser? usr = null)
    {
        if (CurrentOfflineUser is not null)//&& CurrentOfflineUser.IsAuthenticated && usr == null)
        {
            return CurrentOfflineUser;
        }
        db = Realm.GetInstance(DataBaseService.GetRealm());
        List<UserModel>? dbUser = db.All<UserModel>().ToList();
        if (dbUser is null)
        {
            return null;
        }
        UserModel? usrr = dbUser.FirstOrDefault();
        if (usrr is not null)
        {
            if (usrr.UserPassword is null)
            {
                return null;
            }
            if (usr is not null)
            {
                CurrentOfflineUser = new UserModelView(usr);
                db.Write(() =>
                {
                    UserModel user = new(CurrentOfflineUser);

                    db.Add(user, true);
                });
                return CurrentOfflineUser;

            }
            ;
            CurrentOfflineUser = new UserModelView(usrr);

        }
        //CurrentOfflineUser = new(dbUser);
        return CurrentOfflineUser;
    }



    public static bool InitializeParseClient()
    {

        try
        {
            if (ParseClient.Instance is not null)
            {
                return true;
            }
            // Check for internet connection
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                Debug.WriteLine("No Internet Connection: Unable to initialize ParseClient.");
                return false;
            }

            // Validate API Keys
            if (string.IsNullOrEmpty(APIKeys.ApplicationId) ||
                string.IsNullOrEmpty(APIKeys.ServerUri) ||
                string.IsNullOrEmpty(APIKeys.DotNetKEY))
            {
                Debug.WriteLine("Invalid API Keys: Unable to initialize ParseClient.");
                return false;
            }

            // Create ParseClient
            ParseClient client = new ParseClient(new ServerConnectionData()
            {
                ApplicationID = APIKeys.ApplicationId,
                ServerURI = APIKeys.ServerUri,
                Key = APIKeys.DotNetKEY,
            }
            );

            HostManifestData manifest = new HostManifestData()
            {
                Version = "1.6.0",
                Identifier = "com.yvanbrunel.dimmer",
                Name = "Dimmer",
            };
            client.Publicize();

            Debug.WriteLine("ParseClient initialized successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing ParseClient: {ex.Message}");
            return false;
        }
    }
    #endregion
}
