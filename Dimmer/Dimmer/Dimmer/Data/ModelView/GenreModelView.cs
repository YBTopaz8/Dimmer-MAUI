namespace Dimmer.Data.ModelView;
public partial class GenreModelView : ObservableObject
{
    [ObservableProperty]
    public partial ObjectId Id { get; set; }
    [ObservableProperty]
    public partial string? Name { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentlySelected { get; set; }
    
}
