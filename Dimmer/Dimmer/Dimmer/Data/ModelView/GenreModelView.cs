namespace Dimmer.Data.ModelView;
public partial class GenreModelView : ObservableObject
{
    [ObservableProperty]
    public partial string? LocalDeviceId { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string? Name { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentlySelected { get; set; }
    
}
