using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DimmerParse.ParseStatics;
public class ApiKeys
{
    // Static fields to hold keys  
    private static string? _dotNetKEY = "yJyMksGqqf23tC8B4gjJMh4N2FaC3u2LNr7wJ3KA";

    public static string? DotNetKEY
    {
        get => _dotNetKEY;
        private set => _dotNetKEY = value;
    }

    public static string? ApplicationId = "6AhkrgrFyppJGEgIpyXFK4z7wxusjmqgAuN5Fogd";  // Replace with your actual App ID  

    public static string? ServerUri = "https://dimmer.b4a.io/";  // Back4App server URL  

    public static string? LASTFM_API_KEY = "30f6e9d291e37df60a62f5c3ae32379e";
    public static string? LASTFM_API_SECRET = "7bab4a5c5db5ee1ef02bf806d84c19a9";
    public static string? LASTFM_USERNAME = "YBTopaz8";
    public static string? LASTFM_PASSWORD = "i&9MssL+N)Jm4G/";

    public static string? ParseEmail = "8brunel@gmail.com";
    public static string? ParseUsername = "YBTopaz8";
    public static string? ParsePassword = "Yvan";

}
