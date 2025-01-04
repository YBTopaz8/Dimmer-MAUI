using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.DataAccess.Services;
public partial class SongsManagementService
{

    #region Online Region

    public void UpdateUserLoginDetails(ParseUser usrr)
    {
        CurrentUserOnline = usrr;
        CurrentOfflineUser.UserEmail = usrr.Username;        
        CurrentOfflineUser.LastSessionDate = (DateTimeOffset)usrr.UpdatedAt!;        
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
            _ = ParseClient.Instance.LogInWithAsync(email, password);

            // Check if the email is verified (if applicable)
            if (CurrentUserOnline is not null)
            {
                if (await CurrentUserOnline.IsAuthenticatedAsync())
                {
                    return true;
                }

            }
            var user = await ParseClient.Instance.GetCurrentUser();
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
        var user = await ParseClient.Instance.GetCurrentUser();

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
            var result = (InitializeParseClient());
            if (CurrentOfflineUser is null)
            {
                CurrentOfflineUser = new UserModelView()
                {
                    UserName = string.Empty,
                    UserEmail = string.Empty,
                    UserPassword = string.Empty,
                };
            }
            if (CurrentUserOnline is null)
            {
                // display user is offline
                //await Shell.Current.DisplayAlert("Hey!", "Please login to save your songs", "Ok");
                return null;
            }

            if (await CurrentUserOnline.IsAuthenticatedAsync())
            {
                return null;
            }
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                UserModel? user = new();
                user.UserEmail = CurrentUserOnline.Email;
                user.UserName = CurrentUserOnline.Username;
                user.UserPassword = CurrentUserOnline.Password;


                var userdb = db.All<UserModel>();
                if (userdb.Any())
                {
                    var usr = userdb.FirstOrDefault()!;
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
            await Shell.Current.DisplayAlert("Hey!", ex.Message, "Ok");
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
            var user = await ParseClient.Instance.GetCurrentUser();
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
            var user = await ParseClient.Instance.GetCurrentUser();

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

}
