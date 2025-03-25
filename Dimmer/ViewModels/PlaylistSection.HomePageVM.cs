using System.Threading.Tasks;

namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM
{
    


    CancellationTokenSource cts = new();
    const string songAddedToPlaylistText = "Song Added to AlbumsQueue";
    const string songDeletedFromPlaylistText = "Song Removed from AlbumsQueue";
    const string PlaylistCreatedText = "AlbumsQueue Created Successfully!";
    const string PlaylistDeletedText = "AlbumsQueue Deleted Successfully!";
    const ToastDuration duration = ToastDuration.Short;

    
   

}