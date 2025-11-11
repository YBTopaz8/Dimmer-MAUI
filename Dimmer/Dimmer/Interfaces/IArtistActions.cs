using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces;

public interface IArtistActions
{
    void QuickViewArtist(string artistName);
    void PlaySongsByArtistInCurrentAlbum(string artistName);
    void PlayAllSongsByArtist(string artistName);
    void QueueAllSongsByArtist(string artistName);
    void NavigateToArtistPage(string artistName);
    bool IsArtistFavorite(string artistName);
    void ToggleFavoriteArtist(string artistName, bool isFavorite);
    // Optional stats
    int GetArtistPlayCount(string artistName);        // return 0 if unknown
    bool IsArtistFollowed(string artistName);          // if you have the concept
}
