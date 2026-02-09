using Dimmer.Utils;

using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Infrastructure;

using System.Net;

using NetworkAccess = Microsoft.Maui.Networking.NetworkAccess;

namespace Dimmer.DimmerLive.Orchestration;
public static class ParseSetup
{
    public static class YBParse
    {
        public static string? ApplicationId { get;  set; } 
        public static string? ServerUri { get;  set; } 
        public static string? DotNetKEY { get;  set; } 


    }


    public static bool InitializeParseClient()
    {
          try
        {
        
            
            // Validate API Keys
            if (string.IsNullOrEmpty(YBParse.ApplicationId) || // PUT IN YOUR APP ID HERE
                string.IsNullOrEmpty(YBParse.ServerUri) || // PUT IN YOUR ServerUri ID HERE
                string.IsNullOrEmpty(YBParse.DotNetKEY)) // PUT IN YOUR DotNetKEY ID HERE
                                                         //You can use your Master Key instead of DOTNET but beware as it is the...Master Key
            {
                Console.WriteLine("Invalid API Keys: Unable to initialize ParseClient.");
                return false;
            }
            
            // Create ParseClient
            ParseClient client = new ParseClient(new ServerConnectionData
            {
                ApplicationID = YBParse.ApplicationId,
                ServerURI = YBParse.ServerUri,
                Key = YBParse.DotNetKEY,

            }
            );
          

            client.Publicize();


            Debug.WriteLine("ParseClient initialized successfully.!!!!");
            return ParseClient.Instance is not null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing ParseClient: {ex.Message}");
            return false;
        }
    }

    public static async Task<ParseUser?> SignUpUserAsync(ParseUser CurrentUser)
    {
        if (string.IsNullOrEmpty(CurrentUser.Email) || string.IsNullOrEmpty(CurrentUser.Password))
        {            
            return null;
        }
        try
        {
            if (!CurrentUser.Email.Contains('@'))
            {                
                return null;
            }

            var usr = await ParseClient.Instance.SignUpWithAsync(CurrentUser);

            if (usr == null)
            {
                Console.WriteLine("User registration failed.");
                return null;
            }
            CurrentUser.Password = null; // Clear password after registration
            CurrentUser.ObjectId = usr.ObjectId;
            
            return CurrentUser;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error when registering user: {ex.Message}");
            return null;
        }

    }


    public static async Task<bool> LogInParseOnline(ParseUser CurrentUser, bool isSilent = true)
    {
        if (CurrentUser is null)
        {
            return false;
        }

        if ((string.IsNullOrEmpty(CurrentUser.Password)||string.IsNullOrEmpty(CurrentUser.Username)) && !isSilent)
        {
            await Shell.Current.DisplayAlert("Error!", "Empty UserName/Password", "Ok");

            return false;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet && !isSilent)
        {
            if (!isSilent)
            {
                await Shell.Current.DisplayAlert("Error!", "No Internet Connection", "Ok");

                return false;
            }
            else
            {
                return false;
            }

        }
        if ((string.IsNullOrEmpty(CurrentUser.Username) || string.IsNullOrEmpty(CurrentUser.Password)) && !isSilent)
        {
            if (!isSilent)
            {
                await Shell.Current.DisplayAlert("Error!", "Please Verify Email/Password", "Ok");

                return false;
            }
            else
            {
                return false;
            }

        }
        try
        {

            ParseUser? currentParseUser = await ParseClient
                .Instance
                .LogInWithAsync(CurrentUser.Username, CurrentUser.Password);
            

            CurrentUser.Password= currentParseUser.Password;
            return true;
        }
        catch (Exception ex)
        {
            if (ex.Source == "System.Net.Http")
            {
                await Shell.Current.DisplayAlert("Error!", "Invalid Credentials", "Ok");

            }
            return false;
        }
        
    }


    public static bool LogUserOut()
    {
        ParseClient.Instance.LogOut();
        return true;
    }

    public static async Task<bool> ForgottenPasswordRequest()
    {
        await ParseClient.Instance.RequestPasswordResetAsync((await ParseClient.Instance.GetCurrentUser()).Email);

        return true;

    }

}
