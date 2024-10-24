namespace Dimmer_MAUI.Utilities.Services.Models
{
    public class ArtistModel : RealmObject
    {
        [PrimaryKey]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; }
        public string? ImagePath { get; set; }

        public ArtistModel()
        {
        }

        public ArtistModel(ArtistModelView modelView)
        {
            Id = modelView.Id;
            Name = modelView.Name;
            ImagePath = modelView.ImagePath;        
        }
    }

    // ViewModel for ArtistModel
    public partial class ArtistModelView : ObservableObject
    {
        public ObjectId Id { get; set;}

        [ObservableProperty]
        string name;
        [ObservableProperty]
        string? imagePath;
        [ObservableProperty]
        bool isCurrentlySelected = false;
        public ArtistModelView()
        {
        }

        public ArtistModelView(ArtistModel model)
        {
            Id = model.Id;
            Name = model.Name;
            ImagePath = model.ImagePath;
        }
    }
}