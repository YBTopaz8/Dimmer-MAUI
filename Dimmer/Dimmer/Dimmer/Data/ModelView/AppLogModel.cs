using Dimmer.DimmerLive.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;
public class AppLogModel
{
    public string Log { get; set; } = string.Empty;
    public SongModel? AppSongModel { get; set; } = new SongModel();
    public SongModelView? ViewSongModel { get; set; } = new SongModelView();
    public UserModelView? UserModel { get; set; }
    public UserDeviceSession? DeviceModelSession { get; set; }
}
