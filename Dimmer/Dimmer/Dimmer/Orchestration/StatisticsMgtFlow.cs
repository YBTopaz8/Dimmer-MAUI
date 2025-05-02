using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Orchestration;
public class StatisticsMgtFlow : BaseAppFlow, IDisposable
{
    public StatisticsMgtFlow(IPlayerStateService state, IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo, 
        IRepository<AlbumArtistGenreSongLink> aagslRepo, 
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo, 
        IRepository<PlaylistModel> playlistRepo, IRepository<ArtistModel> artistRepo, 
        IRepository<AlbumModel> albumRepo, ISettingsService settings, 
        IFolderMgtService folderMonitor, IMapper mapper) : base(state, songRepo, genreRepo, aagslRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, mapper)
    {
    }
}
