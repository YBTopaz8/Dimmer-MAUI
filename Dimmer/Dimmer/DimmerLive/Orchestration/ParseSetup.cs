using Parse.Infrastructure;

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
            });
          

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

  

}
