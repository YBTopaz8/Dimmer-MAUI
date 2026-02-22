using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;

public static class DataTypeExtension
{
    public static string FromLongBytesToStringMB(this long value)
    {
        
        if (value is long vl)
        {
            return (vl / 1024.0 / 1024.0).ToString("0.##") + " MB";
        }
        

        return "0 MB"; // Default case if conversion fails
    }

}

