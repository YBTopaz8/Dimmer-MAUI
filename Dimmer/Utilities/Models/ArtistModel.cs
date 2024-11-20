namespace Dimmer_MAUI.Utilities.Services.Models
{
    public partial class ArtistModel : RealmObject
    {
        [PrimaryKey]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; } = "Unknown Artist";
        public string Bio { get; set; }
        public string? ImagePath { get; set; }
        public ObjectId UserId { get; set; }
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
        string? bio;    
        [ObservableProperty]
        bool isCurrentlySelected = false;
        partial void OnIsCurrentlySelectedChanged(bool oldValue, bool newValue)
        {
            oldValue = false;
            OnPropertyChanged(nameof(IsCurrentlySelected));
        }
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