using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;
public class FolderModel
{
    public string FolderName { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public string DisplayName { get; set; }
    public string Path { get; set; }
}
