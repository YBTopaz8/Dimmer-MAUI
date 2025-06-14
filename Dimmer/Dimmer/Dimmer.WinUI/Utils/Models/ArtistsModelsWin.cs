namespace Dimmer.WinUI.Utils.Models;

public partial class ArtistModelWin : ArtistModelView
{
    public bool IsNewOrModifiedWin { get; set; }

    public ObservableCollection<AlbumModelView> ArtistAlbums { get; set; } = new ObservableCollection<AlbumModelView>();

}
