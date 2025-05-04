
using Parse.Infrastructure;
using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dimmer.DimmerLive.ParseStatics.ParseStatics;
using System.Diagnostics;
using Dimmer.DimmerLive.ParseStatics;
using Dimmer.DimmerLive.Models;


namespace Dimmer.DimmerLive.Orchestration;
public static class ParseSetup
{


    public static bool InitializeParseClient()
    {
        try
        {
            // Validate API Keys
            if (string.IsNullOrEmpty(ApiKeys.ApplicationId) || // PUT IN YOUR APP ID HERE
                string.IsNullOrEmpty(ApiKeys.ServerUri) || // PUT IN YOUR ServerUri ID HERE
                string.IsNullOrEmpty(ApiKeys.DotNetKEY)) // PUT IN YOUR DotNetKEY ID HERE
                                                         //You can use your Master Key instead of DOTNET but beware as it is the...Master Key
            {
                Console.WriteLine("Invalid API Keys: Unable to initialize ParseClient.");
                return false;
            }

            // Create ParseClient
            ParseClient client = new ParseClient(new ServerConnectionData
            {
                ApplicationID = ApiKeys.ApplicationId,
                ServerURI = ApiKeys.ServerUri,
                Key = ApiKeys.DotNetKEY,

            }
            );
            //HostManifestData manifest = new HostManifestData()
            //{
            //    Version = "1.0.0",
            //    Identifier = "com.yvanbrunel.flowhub",
            //    Name = "Flowhub",
            //};

            client.Publicize();


            Console.WriteLine("ParseClient initialized successfully.");
            return true;
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
