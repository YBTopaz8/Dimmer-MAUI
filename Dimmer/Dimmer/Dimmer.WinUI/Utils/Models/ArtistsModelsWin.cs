using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Data.Models;

using MongoDB.Bson;

using Realms;

namespace Dimmer.WinUI.Utils.Models;

public partial class ArtistModelWin : ArtistModelView
{
    public bool IsNewOrModifiedWin { get; set; }

    public ObservableCollection<AlbumModelView> ArtistAlbums { get; set; } = new ObservableCollection<AlbumModelView>();

}
