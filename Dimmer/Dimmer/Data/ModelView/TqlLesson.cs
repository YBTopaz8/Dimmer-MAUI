using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;
public partial class TqlLesson : ObservableObject
{
    [ObservableProperty]
    public partial string Category{ get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Explanation{ get; set; }

    [ObservableProperty]
    public partial string TqlQuery{ get; set; }
}