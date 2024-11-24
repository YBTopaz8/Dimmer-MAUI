namespace Dimmer_MAUI.Utilities.Models;

public partial class ArtistModel : RealmObject
{
    public BaseEmbedded Instance { get; set; } // = new ();

    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(ArtistModel));
    public string? Name { get; set; } = "Unknown Artist";
    public string? Bio { get; set; }
    public string? ImagePath { get; set; }
           
    
    public ArtistModel()
    {
        Instance = new BaseEmbedded();

    }

    public ArtistModel(ArtistModelView model)
    {
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
        Name = model.Name;
        ImagePath = model.ImagePath;
        Bio = model.Bio;
    }

}

// ViewModel for ArtistModel
public partial class ArtistModelView : ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(ArtistModelView));
    [ObservableProperty]
    BaseEmbeddedView instance = new();
    [ObservableProperty]
    string? name;
    [ObservableProperty]
    string? imagePath;
    [ObservableProperty]
    string? bio;
    [ObservableProperty]
    bool isCurrentlySelected;
    public ArtistModelView()
    {
    }

    public ArtistModelView(ArtistModel model)
    {
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
        Name = model.Name;
        ImagePath = model.ImagePath;     
        Bio = model.Bio;
        
    }


}