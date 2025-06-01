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


    //public ObjectId Id { get; set; }
    //public string? Name { get; set; } = "Unknown Artist";
    //public string? Bio { get; set; }
    //public string? ImagePath { get; set; } = "lyricist.png";
    //public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;


    //public ObservableCollection<TagModel> Tags { get; }
    //public ObservableCollection<UserNoteModel> UserNotes { get; }
    //public ArtistModelWin()
    //{

    //}
    //public ArtistModelWin(AlbumModel albumModel)
    //{
    //    Id= albumModel.Id;
    //    Name = albumModel.Name;
    //    Bio = albumModel.Description;
    //    ImagePath = albumModel.ImagePath ?? "lyricist.png";

    //    DateCreated = albumModel.DateCreated ?? DateTimeOffset.UtcNow;
    //    Tags= new ObservableCollection<TagModel>(albumModel.Tags ?? new List<TagModel>());
    //    UserNotes = new ObservableCollection<UserNoteModel>(albumModel.UserNotes ?? new List<UserNoteModel>());

    //}


}
