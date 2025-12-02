namespace Dimmer.Data.ModelView;

[Utils.Preserve(AllMembers = true)]
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