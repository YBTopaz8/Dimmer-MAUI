using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities;
public class HttpClientSingleton
{
    static readonly HttpClient _httpClient = new();
    public static HttpClient Instance => _httpClient;

}
