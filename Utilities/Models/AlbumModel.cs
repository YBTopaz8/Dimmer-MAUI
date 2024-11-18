namespace Dimmer_MAUI.Utilities.Services.Models
{
    public class AlbumModel : RealmObject
    {
        [PrimaryKey]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; }
        public int? ReleaseYear { get; set; }
        //property for number of tracks
        public int NumberOfTracks { get; set; }
        public string? ImagePath { get; set; }

        public AlbumModel()
        {
        
        }

        public AlbumModel(AlbumModelView modelView)
        {
            Id = modelView.Id;
            Name = modelView.Name;
            ReleaseYear = modelView.ReleaseYear;
            NumberOfTracks = modelView.NumberOfTracks;
            ImagePath = modelView.AlbumImagePath;
        }
    }

    public class AlbumArtistSongLink : RealmObject
    {
        [PrimaryKey]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public ObjectId ArtistId { get; set; }
        public ObjectId SongId { get; set; }
        public ObjectId AlbumId { get; set; }
    }

    public partial class AlbumModelView : ObservableObject
    {
        public ObjectId Id { get; set; }
        [ObservableProperty]
        string name;
        [ObservableProperty]
        int? releaseYear;
        //property for number of tracks
        [ObservableProperty]
        int numberOfTracks;
        [ObservableProperty]
        string totalDuration;
        [ObservableProperty]
        string? albumImagePath;
        [ObservableProperty]
        bool isCurrentlySelected = false;
        public AlbumModelView(AlbumModel model)
        {
            Id = model.Id;
            Name = model.Name;
            NumberOfTracks = model.NumberOfTracks;
            AlbumImagePath = model.ImagePath;
        }

        public AlbumModelView()
        {
        
        }

    }
}
