using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DimmerParse.Models;
[ParseClassName("UserModelOnline")]
public class UserModelOnline : ParseUser
{
    [ParseFieldName("sessionId")]
    public string UserName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    
    public string 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
}
