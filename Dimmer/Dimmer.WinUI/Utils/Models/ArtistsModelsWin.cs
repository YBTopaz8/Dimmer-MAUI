namespace Dimmer.WinUI.Utils.Models;

public partial class ArtistModelWin : ArtistModelView
{
    public bool IsNewOrModifiedWin { get; set; }

    public ObservableCollection<SongModelView> ArtistAlbums { get; set; } = new ObservableCollection<SongModelView>();

}
