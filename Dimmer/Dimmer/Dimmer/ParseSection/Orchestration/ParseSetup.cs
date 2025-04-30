using Dimmer.Utils;
using Parse.Infrastructure;
using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dimmer.ParseSection.ParseStatics.ParseStatics;
using Dimmer.ParseSection.Models;

namespace Dimmer.ParseSection.Orchestration;
static class ParseSetup
{


    public static bool InitializeParseClient()
    {
        try
        {
            // Check for internet connection
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                Console.WriteLine("No Internet Connection: Unable to initialize ParseClient.");
                return false;
            }

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


            Debug.WriteLine("ParseClient initialized successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing ParseClient: {ex.Message}");
            return false;
        }
    }
    public static async Task<bool> DeleteMessageAsync(ChatMessage message, bool softDelete = true)
    {
        if (message == null || string.IsNullOrEmpty(message.ObjectId))
            return false;

        // Optional: Check if current user is the sender or has permission
        // if (message.Sender?.ObjectId != GetCurrentUser()?.ObjectId) return false;

        try
        {
            if (softDelete)
            {
                message.IsDeleted = true;
                // Maybe clear content? message.Text = null; message.AttachmentFile = null; etc.
                await message.SaveAsync();
                Debug.WriteLine($"Message {message.ObjectId} soft deleted.");
            }
            else
            {
                await message.DeleteAsync(); // Permanent deletion
                Debug.WriteLine($"Message {message.ObjectId} permanently deleted.");
            }
            // Note: Live Query 'delete' event should fire for permanent deletion.
            // For soft delete, an 'update' event will fire. Handle accordingly in UI.
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting message {message.ObjectId}: {ex.Message}");
            return false;
        }
    }

    // Optional: Editing text messages
    public static async Task<bool> UpdateTextMessageAsync(ChatMessage message, string newText)
    {
        if (message == null || string.IsNullOrEmpty(message.ObjectId) || message.MessageType != MessageTypes.Text)
            return false;
        // Optional: Check permissions
        // if (message.Sender?.ObjectId != GetCurrentUser()?.ObjectId) return false;

        try
        {
            message.Text = newText;
            // Optionally add an 'isEdited' flag
            await message.SaveAsync();
            Debug.WriteLine($"Message {message.ObjectId} updated.");
            // Live Query 'update' event will fire.
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating message {message.ObjectId}: {ex.Message}");
            return false;
        }
    }


    // --- Music Specific ---

 
}
