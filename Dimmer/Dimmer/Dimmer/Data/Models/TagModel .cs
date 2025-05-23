using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.Models;
public partial class TagModel : RealmObject
{
    [PrimaryKey] public ObjectId Id { get; set; }
    public string Name { get; set; }
    public IList<SongModel> Songs { get; }
}