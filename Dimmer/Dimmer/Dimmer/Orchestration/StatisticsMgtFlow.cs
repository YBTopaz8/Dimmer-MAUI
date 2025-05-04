using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Orchestration;
public class StatisticsMgtFlow : BaseAppFlow, IDisposable
{
    public StatisticsMgtFlow(IDimmerStateService state, IRepository<SongModel> songRepo,
        IRepository<UserModel> userRepo,
        IRepository<GenreModel> genreRepo, 
        IRepository<AlbumArtistGenreSongLink> aagslRepo, 
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo, 
        IRepository<PlaylistModel> playlistRepo, IRepository<ArtistModel> artistRepo, 
        IRepository<AlbumModel> albumRepo, ISettingsService settings, 
        IFolderMgtService folderMonitor, IMapper mapper, SubscriptionManager subs) : base(state, songRepo, genreRepo,userRepo, aagslRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, subs,mapper)
    {
    }
}
