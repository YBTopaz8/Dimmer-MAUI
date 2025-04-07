namespace Dimmer.Data.ModelView;
public partial class SelectedSongModelView : SongModelView
{
    
    [ObservableProperty]
    public partial bool HasCoverImage { get; set; }
    //i need ctor
    
}
