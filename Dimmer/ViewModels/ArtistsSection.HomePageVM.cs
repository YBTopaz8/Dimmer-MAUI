//using WinRT;

using System.Linq;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
    [ObservableProperty]
    public partial string? SearchPlaceHolder { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView>? AllArtists { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<ArtistGroup>? GroupedArtists { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<AlbumGroup>? GroupedAlbums { get; set; } = new();
    [ObservableProperty]
    public partial ArtistGroup? SelectedGroupedArtist { get; set; } = new();[ObservableProperty]
    public partial AlbumGroup? SelectedGroupedAlbum { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView>? AllAlbums{get;set;}= new();
    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? AllArtistsAlbumSongs {get;set;}= new();
    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? AllArtistsSongs {get;set;}= new();
    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView>? AllArtistsAlbums {get;set;}= new();
    [ObservableProperty]
    public partial string? SelectedArtistPageTitle { get; set; }

    [ObservableProperty]
    public partial AlbumModelView? SelectedAlbumOnArtistPage { get; set; }
    [ObservableProperty]
    public partial ArtistModelView? SelectedArtistOnArtistPage { get; set; }
    [ObservableProperty]
    public partial AlbumModelView? SelectedAlbumOnAlbumPage { get; set; }
    [ObservableProperty]
    public partial ArtistModelView? SelectedArtistOnAlbumPage { get; set; }

    [RelayCommand]
    public async Task NavigateToSpecificAlbumPageFromBtmSheet(SongModelView song)
    {
        MySelectedSong = song;
        var songAlbum = GetAlbumFromSongID(song.LocalDeviceId!).FirstOrDefault();
        
        await NavigateToSpecificAlbumPage(songAlbum);

    }

    
    [RelayCommand]
    async Task NavigateToSpecificAlbumPage(AlbumModelView selectedAlbum)
    {
        SelectedArtistOnArtistPage = GetAllArtistsFromAlbumID(selectedAlbum.LocalDeviceId).FirstOrDefault();
        SelectedAlbumOnArtistPage = AllAlbums.First(x => x.LocalDeviceId == selectedAlbum.LocalDeviceId);
        //SelectedAlbumOnArtistPage.NumberOfTracks = SongsMgtService.GetSongsCountFromAlbumID(selectedAlbum.LocalDeviceId);
        SelectedAlbumOnArtistPage.AlbumImagePath = SongsMgtService.AllSongs.First(x => x.AlbumName == SelectedAlbumOnArtistPage.Name).CoverImagePath;
        SelectedAlbumOnArtistPage.TotalDuration = TimeSpan
            .FromSeconds(SongsMgtService.AllSongs
            .Where(x => x.AlbumName == SelectedAlbumOnArtistPage.Name)
            .Sum(x => x.DurationInSeconds))
            .ToString(@"mm\:ss");

        await Shell.Current.GoToAsync(nameof(SpecificAlbumPage));
        await ShowSpecificArtistsSongsWithAlbum(selectedAlbum);
    }
   
    [RelayCommand]
    public async Task NavigateToArtistsPage(int? callerID=0) //0 if called by else, 1 if called by homeD or homeM
    {

        if (callerID == 0)
        {
            MySelectedSong = TemporarilyPickedSong!;
        }
       
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(ArtistsPageD));
#elif ANDROID
        await Shell.Current.GoToAsync(nameof(ArtistsPageM));
        SelectedArtistOnArtistPage = GetAllArtistsFromSongID(MySelectedSong!.LocalDeviceId!).FirstOrDefault();
        LoadArtistAlbumsAndSongs(SelectedArtistOnArtistPage);
#endif

    }
    public void LoadArtistSongs()
    {
        if (SelectedArtistOnArtistPage is not null && SelectedArtistOnArtistPage.Name == MySelectedSong.ArtistName)
        {
            if (AllArtistsAlbumSongs.Count > 1)
            {
                return;
            }
        }
        SelectedArtistOnArtistPage = GetAllArtistsFromSongID(MySelectedSong!.LocalDeviceId!).FirstOrDefault();
        LoadArtistAlbumsAndSongs(SelectedArtistOnArtistPage);
    }
    [RelayCommand]
    public async Task NavigateToAlbumsPage(int? callerID=0) //0 if called by else, 1 if called by homeD or homeM
    {

        if (callerID == 0)
        {
            MySelectedSong = TemporarilyPickedSong!;
        }
       
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(AlbumsPageD));
#elif ANDROID
        await Shell.Current.GoToAsync(nameof(AlbumPageM));
        
#endif

    }
    
    public void GetAllAlbums()
    {
        AllAlbums = PlayBackService.GetAllAlbums();
        if (MySelectedSong is null)
        {
            MySelectedSong = TemporarilyPickedSong;
        }
        var groupedSongs = DisplayedSongs.GroupBy(song => song.AlbumName);

        foreach (var group in groupedSongs)
        {
            var firstSong = group.FirstOrDefault();
            // Create an AlbumGroup and add all the songs in the grAlbumGroup?AlbumGroup?AlbumGroup? to it.
            var AlbumGroup = new AlbumGroup(
                group.Key, // AlbumName
                firstSong.AlbumName, // Use Album name as ID since that's what you grouped by                
                group.ToObservableCollection()
            )!;

            if (GroupedAlbums is not null)
            {
                GroupedAlbums.Add(AlbumGroup);
                List<string> firstLetters = [.. GroupedAlbums
                  .Select(AlbumGroup =>
                  {
                      string firstChar = (AlbumGroup.AlbumName ?? string.Empty).Trim().ToUpper(); // Handle null/empty
                      if (string.IsNullOrEmpty(firstChar))
                      {
                          return "&"; // Handle empty Album names
                      }

                      firstChar = firstChar.Substring(0, 1); // Take only the first character


                      if (char.IsDigit(firstChar[0]))
                      {
                          return "#"; // Numbers
                      }
                      else if (!char.IsLetterOrDigit(firstChar[0]))
                      {
                          return "&"; // Non-alphanumeric
                      }
                      else
                      {
                          return firstChar; // Letters
                      }
                  })
                  .Distinct() // Remove duplicates
                  .OrderBy(letter => letter)];

                //Now you have the `firstLetters` list, for instance bound to other control.
                //Example
                GroupedAlbumNames = firstLetters;
            }
        }
        if (GroupedAlbums is null)
        {
            return;
        }
        SelectedGroupedAlbum = GroupedAlbums.Where(x => x.AlbumName == TemporarilyPickedSong?.AlbumName).FirstOrDefault();

       
        if (AllAlbums.Count > 0)
        {

            SelectedAlbumOnAlbumPage ??= AllAlbums.First(x=>x.Name == MySelectedSong.AlbumName);

        }
        
    }

    [ObservableProperty]
    public partial List<string>? GroupedArtistNames { get; set; }
    [ObservableProperty]
    public partial List<string>? GroupedAlbumNames { get; set; }
    [ObservableProperty]
    public partial List<string>? GroupedSongNames { get; set; }
    [RelayCommand]
    void GetAllArtists()
    {    

        // Group songs by ArtistName (you could also group by ArtistId)
        var groupedSongs = DisplayedSongs.GroupBy(song => song.ArtistName);

        foreach (var group in groupedSongs)
        {
            var firstSong = group.FirstOrDefault();
                                                              // Create an ArtistGroup and add all the songs in the group to it.
            var artistGroup = new ArtistGroup(
                group.Key, // ArtistName
                firstSong.ArtistName, // Use Artist name as ID since that's what you grouped by                
                group.ToObservableCollection()
            );

            if (GroupedArtists is not null)
            {
                GroupedArtists.Add(artistGroup);
                List<string> firstLetters = [.. GroupedArtists
                  .Select(artistGroup =>
                  {
                      string firstChar = (artistGroup.ArtistName ?? string.Empty).Trim().ToUpper(); // Handle null/empty
                      if (string.IsNullOrEmpty(firstChar))
                      {
                          return "&"; // Handle empty artist names
                      }

                      firstChar = firstChar.Substring(0, 1); // Take only the first character


                      if (char.IsDigit(firstChar[0]))
                      {
                          return "#"; // Numbers
                      }
                      else if (!char.IsLetterOrDigit(firstChar[0]))
                      {
                          return "&"; // Non-alphanumeric
                      }
                      else
                      {
                          return firstChar; // Letters
                      }
                  })
                  .Distinct() // Remove duplicates
                  .OrderBy(letter => letter)];

                            //Now you have the `firstLetters` list, for instance bound to other control.
                            //Example
                            GroupedArtistNames = firstLetters;
                        }
                    }
        if (GroupedArtists is null)
        {
            return;
        }
        SelectedGroupedArtist = GroupedArtists.Where(x=>x.ArtistName == TemporarilyPickedSong?.ArtistName).FirstOrDefault();

            if (AllArtists?.Count != PlayBackService.GetAllArtists().Count)
            {
                AllArtists = PlayBackService
                    .GetAllArtists()
                    .OrderBy(x => x.Name)
                    .ToObservableCollection();
                if (AllArtists.Count > 0)
                {
#if WINDOWS
                    if (SelectedArtistOnArtistPage is not null)
                    {
                        SelectedArtistOnArtistPage.IsCurrentlySelected = true;
                        var song = DisplayedSongs.FirstOrDefault(x => x.ArtistName == SelectedArtistOnArtistPage.Name);
                        LoadAllArtistsAlbumsAndLoadAnAlbumSong(song: song, isFromSong: true);

                    }
#endif
                }
            }

            //if (SelectedAlbumOnArtistPage is not null)
            //{
            //    foreach (var album in AllArtistsAlbums)
            //        album.IsCurrentlySelected = album.Name == SelectedAlbumOnArtistPage.Name;
            //}
        }

    public void LoadAllArtistsAlbumsAndLoadAnAlbumSong(AlbumModelView? album = null, SongModelView? song = null, bool isFromSong = false)
    {
        if (DisplayedSongs is null)
        {
            return;
        }
        if(SelectedAlbumOnAlbumPage is not null)
            SelectedAlbumOnAlbumPage.IsCurrentlySelected = false;
        if (isFromSong)
        {
            if (song is null)
            {
                return;
            }
            var AllArtistsFromSong = GetAllArtistsFromSongID(song.LocalDeviceId);
            SelectedArtistOnAlbumPage = AllArtistsFromSong.Count > 0 ? AllArtistsFromSong.FirstOrDefault() : null;
            if (SelectedArtistOnAlbumPage is not null)
            {
                SelectedArtistOnAlbumPage.IsCurrentlySelected = true;
            }
            else
            {
                if (string.IsNullOrEmpty(song.FilePath))
                {
                    return;
                }
                SongsMgtService.LoadSongsFromFolderAsync([song.FilePath]);
                
            }
            
        }
        else if(album is not null)
        {
            SelectedAlbumOnAlbumPage = album;
            SelectedAlbumOnAlbumPage = AllAlbums!.Where(x => x.LocalDeviceId== album.LocalDeviceId!).FirstOrDefault();
            SelectedAlbumOnAlbumPage.IsCurrentlySelected = true;
            
            if (SelectedAlbumOnAlbumPage is null)
            {
                return;
            }
        }
        else
        {
            if (SelectedAlbumOnAlbumPage is null)
            {
                return;
            }
            SelectedAlbumOnAlbumPage = GetAlbumFromSongID(TemporarilyPickedSong!.LocalDeviceId!).FirstOrDefault();
            if (SelectedAlbumOnAlbumPage is null && AllArtists?.Count > 0)
            {
                if (DisplayedSongs.Contains(TemporarilyPickedSong))
                {
                    SelectedAlbumOnAlbumPage = AllAlbums?.FirstOrDefault(x => x.Name == TemporarilyPickedSong!.AlbumName);
                }
            }
            SelectedArtistOnAlbumPage = GetAllArtistsFromSongID(TemporarilyPickedSong!.LocalDeviceId!).FirstOrDefault();
            SelectedArtistOnAlbumPage.IsCurrentlySelected=true;
        }
        if (AllArtists?.Count < 1)
        {
            AllArtists = PlayBackService.GetAllArtists()
                .OrderBy(x => x.Name)
                .ToObservableCollection();

            if (AllArtists?.Count < 1)
            {
                return;
            }
        }

        AllArtistsAlbums?.Clear();
        LoadArtistAlbumsAndSongs(SelectedArtistOnAlbumPage);
        
        //await ShowSpecificArtistsSongsWithAlbum(SelectedAlbumOnAlbumPage!);
        
    }

    [RelayCommand]
    public async Task ShowSpecificArtistsSongsWithAlbum(AlbumModelView album)
    {
        if (AllArtistsAlbums is null || AllArtistsAlbums?.Count<1)
        {
            var selectedLink = AllLinks.FirstOrDefault(link => link.AlbumId == album.LocalDeviceId);
            SelectedArtistOnArtistPage = AllArtists!.FirstOrDefault(artist => artist.LocalDeviceId == selectedLink?.ArtistId)!;

            
            List<string> allAlbums = []; 
            if (SelectedArtistOnArtistPage != null)
            {
                allAlbums = [.. AllLinks
                    .Where(link => link.ArtistId == SelectedArtistOnArtistPage.LocalDeviceId)
                    .Select(link => link.AlbumId)];
            }

            var allAlbumsSet = new HashSet<string>(allAlbums); 
            AllArtistsAlbums = AllAlbums
                .Where(album => allAlbumsSet.Contains(album.LocalDeviceId))
                .ToObservableCollection();
        }
        else
        {
            var selArt= GetAllArtistsFromAlbumID(AllArtistsAlbums.FirstOrDefault()!.LocalDeviceId).FirstOrDefault();
            if (selArt is not null)
            {
                SelectedArtistOnArtistPage = selArt;
            }
        }
        SelectedAlbumOnArtistPage = album;
        if (SelectedAlbumOnArtistPage is null)
        {
            return;
        }
        AllArtistsAlbumSongs= GetAllSongsFromAlbumID(album.LocalDeviceId!);
        SelectedAlbumOnArtistPage.IsCurrentlySelected = true;

        var song = AllArtistsAlbumSongs!.FirstOrDefault();
        if (!string.IsNullOrEmpty(SelectedAlbumOnArtistPage.AlbumImagePath))
        {
            if (!File.Exists(SelectedAlbumOnArtistPage.AlbumImagePath))
            {
                SelectedAlbumOnArtistPage.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(song!.Title, song!.ArtistName!, song!.AlbumName!, song);
            }
        }
        PickedSong = AllArtistsAlbumSongs.FirstOrDefault()!;
    }
    
    public ObservableCollection<ArtistModelView> GetAllArtistsFromSongID(string songId)
  {
        var artistIds = AllLinks
            .Where(link => link.SongId == songId)
            .Select(link => link.ArtistId!)
            .Distinct()
            .ToHashSet();
        if (artistIds is null || artistIds.Count < 1)
        {
            var song= DisplayedSongs.FirstOrDefault(x => x.LocalDeviceId == songId);
            if (song is null)
            {
                return [];
            }
            var artName = song.ArtistName;
            
            var matchingArtist = AllArtists?.FirstOrDefault(x =>
            x.Name.Equals(artName, StringComparison.OrdinalIgnoreCase));

            return matchingArtist is not null
                ? [matchingArtist]
                : [];
        }

        var e = AllArtists.Where(artist => artistIds.Contains(artist.LocalDeviceId!)).ToObservableCollection();
        return e;
    }
    public ObservableCollection<AlbumModelView> GetAlbumFromSongID(string songId)
    {
        // Find album IDs associated with the song
        var albumIds = AllLinks?
            .Where(link => link.SongId == songId)
            .Select(link => link.AlbumId)
            .Where(id => id is not null) // Filter out null IDs
            .Distinct()
            .ToHashSet();

        // Fallback if no album IDs found
        if (albumIds is null || albumIds.Count < 1)
        {
            var albumName = DisplayedSongs.FirstOrDefault(x => x.LocalDeviceId == songId)?.AlbumName;
            if (string.IsNullOrEmpty(albumName))
                return []; // No fallback found

            var matchingAlbum = AllAlbums?.FirstOrDefault(x =>
                x.Name.Equals(albumName, StringComparison.OrdinalIgnoreCase));

            return matchingAlbum is not null
                ? [matchingAlbum]
                : [];
        }

        // Initialize AllAlbums if null
        AllAlbums ??= SongsMgtService.AllAlbums.ToObservableCollection();

        // Filter AllAlbums based on matching IDs
        return [.. AllAlbums.Where(album => albumIds.Contains(album.LocalDeviceId))];
    }

    public ObservableCollection<ArtistModelView> GetAllArtistsFromAlbumID(string albumId)
    {
        var artistIds = AllLinks
            .Where(link => link.AlbumId == albumId && link.ArtistId != null)
            .Select(link => link.ArtistId!)
            .Distinct()
            .ToHashSet();
        AllArtists = SongsMgtService.AllArtists.ToObservableCollection();
        return [.. AllArtists.Where(artist => artistIds.Contains(artist.LocalDeviceId!))];
    }

    public ObservableCollection<SongModelView> GetAllSongsFromAlbumID(string albumId)
    {
        var songIds = AllLinks
            .Where(link => link.AlbumId == albumId && link.SongId != null)
            .Select(link => link.SongId!)
            .Distinct()
            .ToHashSet();

        return [.. DisplayedSongs.Where(song => songIds.Contains(song.LocalDeviceId!))];
    }

    public ObservableCollection<AlbumModelView> GetAllAlbumsFromArtistID(string artistId)
    {
        var albumIds = AllLinks
            .Where(link => link.ArtistId == artistId && link.AlbumId != null)
            .Select(link => link.AlbumId!)
            .Distinct()
            .ToHashSet();
        AllAlbums = SongsMgtService.AllAlbums.ToObservableCollection();
        return [.. AllAlbums.Where(album => albumIds.Contains(album.LocalDeviceId!))];
    }
    public ObservableCollection<AlbumModelView> GetAllAlbumsFromSongID(string songId)
    {
        var albumIds = AllLinks
            .Where(link => link.SongId == songId && link.AlbumId != null)
            .Select(link => link.AlbumId!)
            .Distinct()
            .ToHashSet();
        AllAlbums = SongsMgtService.AllAlbums.ToObservableCollection();
        return [.. AllAlbums.Where(album => albumIds.Contains(album.LocalDeviceId!))];
    }
    public ObservableCollection<SongModelView> GetAllSongsFromArtistID(string artistId)
    {
        var songIds = AllLinks
            .Where(link => link.ArtistId == artistId && link.SongId != null)
            .Select(link => link.SongId!)
            .Distinct()
            .ToHashSet();

        return [.. SongsMgtService.AllSongs.Where(song => songIds.Contains(song.LocalDeviceId!))];
    }

    public void LoadArtistAlbumsAndSongs(ArtistModelView? artist=null)
    {
        //if (artist is null SelectedArtistOnArtistPage is null)
        if (artist is null )
            return;
        if (SelectedArtistOnArtistPage is not null)
            SelectedArtistOnArtistPage.IsCurrentlySelected = false;

        AllArtistsAlbums = GetAllAlbumsFromArtistID(artist.LocalDeviceId!);
        //await GetAlbumsFromArtistIDAsync(artist.LocalDeviceId);
        if (AllArtistsAlbums is null || AllArtistsAlbums.Count<1)
        {
            return;
        }

        SelectedAlbumOnArtistPage = AllArtistsAlbums.FirstOrDefault();
        SelectedArtistOnArtistPage.ImagePath = AllArtistsAlbums.FirstOrDefault().AlbumImagePath;
        
        AllArtistsAlbumSongs= GetAllSongsFromArtistID(artist.LocalDeviceId!);
        SelectedArtistOnArtistPage.IsCurrentlySelected=true;
        if (MySelectedSong is not null)
        {
            MySelectedSong.IsCurrentPlayingHighlight = true;
        }
    }
    
    public void LoadArtistAlbumsAndSongs(AlbumModelView? album=null)
    {
        //if (artist is null SelectedArtistOnArtistPage is null)
        if (album is null )
            return;
        
        if (SelectedAlbumOnAlbumPage is not null)
        {
            SelectedAlbumOnAlbumPage.IsCurrentlySelected = false;
        }
        

        SelectedAlbumOnAlbumPage = album;
        
        AllArtistsAlbumSongs = GetAllSongsFromAlbumID(album.LocalDeviceId!);
        
        if (AllArtistsAlbumSongs is null || AllArtistsAlbumSongs.Count<1)
        {
            return;
        }


        SelectedAlbumOnAlbumPage.IsCurrentlySelected=true;
        if (MySelectedSong is not null)
        {
            MySelectedSong.IsCurrentPlayingHighlight = true;
        }
    }

    [RelayCommand]
    public async Task SetSongCoverAsAlbumCover(SongModelView song)
    {
        var specificAlbum = AllArtistsAlbums.FirstOrDefault(x => x.Name == song.AlbumName)!;
        specificAlbum.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(song.Title, song!.ArtistName!, song!.AlbumName!, song);
        specificAlbum.NumberOfTracks = AllArtistsAlbumSongs.Count;
        SongsMgtService.UpdateAlbum(specificAlbum);
        
    }

    [RelayCommand]
    void SearchArtist(string aName)
    {
        if (CurrentPage == PageEnum.AllArtistsPage)
        {

            if (string.IsNullOrEmpty(aName))
            {
                AllArtists = PlayBackService.GetAllArtists();
                return;
            }
            AllArtists = PlayBackService.GetAllArtists().Where(a => a.Name.Contains(aName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Name).ToObservableCollection();

            if (AllArtists.Count > 0)
            {
                SelectedArtistOnArtistPage = AllArtists.FirstOrDefault();
                var song = DisplayedSongs.FirstOrDefault(x => x.ArtistName == SelectedArtistOnArtistPage.Name);
                LoadAllArtistsAlbumsAndLoadAnAlbumSong(song: song, isFromSong: true);
            
            }
            else
            {
                AllArtistsAlbumSongs?.Clear();
            }

        }
        else
        {

        }
    }
    
    public void SearchAlbum(string aName)
    {
        if (CurrentPage == PageEnum.AllAlbumsPage)
        {

            if (string.IsNullOrEmpty(aName))
            {
                AllAlbums = PlayBackService.GetAllAlbums();
                return;
            }
            AllAlbums = PlayBackService.GetAllAlbums().Where(a => a.Name.Contains(aName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Name).ToObservableCollection();

            if (AllAlbums.Count > 0)
            {
                if (SelectedAlbumOnAlbumPage is null)
                {
                    return;
                }
                SelectedAlbumOnAlbumPage = AllAlbums.FirstOrDefault();
                var song = DisplayedSongs.FirstOrDefault(x => x.AlbumName == SelectedAlbumOnAlbumPage.Name);
                LoadAllArtistsAlbumsAndLoadAnAlbumSong(song: song, isFromSong: true);            
            }
            else
            {
                AllArtistsAlbumSongs?.Clear();
            }

        }
        else
        {

        }
    }
    
    public void FilterArtistList(string FilterLetter, bool isAscending=true)
    {
        if (string.IsNullOrEmpty(FilterLetter))
        {
            AllArtists = PlayBackService.GetAllArtists();
            return;
        }
        if (AllArtists is null)
        {
            return;
        }
        AllArtists.Clear();
        foreach (var art in SongsMgtService.AllArtists)
        {
            if (art.Name is not null)
            {
                if (art.Name.StartsWith(FilterLetter, StringComparison.OrdinalIgnoreCase))
                {
                    
                    AllArtists.Add(art);
                }
                    
            }
        }
        if (AllArtists.Count > 0)
        {
            SelectedArtistOnArtistPage = AllArtists.FirstOrDefault();
            AllArtistsAlbums = GetAllAlbumsFromArtistID(SelectedArtistOnArtistPage.LocalDeviceId!);
            //await GetAlbumsFromArtistIDAsync(artist.LocalDeviceId);
            if (AllArtistsAlbums is null || AllArtistsAlbums.Count<1)
            {
                return;
            }
            SelectedArtistOnArtistPage.ImagePath = AllArtistsAlbums.FirstOrDefault().AlbumImagePath;

            AllArtistsAlbumSongs= GetAllSongsFromArtistID(SelectedArtistOnArtistPage.LocalDeviceId!);
            SelectedArtistOnArtistPage.IsCurrentlySelected=true;
        }
        else
        {
            AllArtistsAlbumSongs?.Clear();
        }
    }
    
    public void FilterAlbumList(string FilterLetter, bool isAscending=true)
    {
        if (string.IsNullOrEmpty(FilterLetter))
        {
            AllAlbums = PlayBackService.GetAllAlbums();
            return;
        }
        if (AllAlbums is null)
        {
            return;
        }
        AllAlbums.Clear();
        foreach (var art in SongsMgtService.AllAlbums)
        {
            if (art.Name is not null)
            {
                if (art.Name.StartsWith(FilterLetter, StringComparison.OrdinalIgnoreCase))
                {
                    
                    AllAlbums.Add(art);
                }
                    
            }
        }
        if (AllAlbums.Count > 0)
        {
            SelectedAlbumOnAlbumPage = AllAlbums.FirstOrDefault();
            
            //await GetAlbumsFromAlbumIDAsync(artist.LocalDeviceId);
            if (AllAlbums is null || AllAlbums.Count<1)
            {
                return;
            }
            SelectedAlbumOnAlbumPage.AlbumImagePath = AllAlbums.FirstOrDefault().AlbumImagePath;

            AllArtistsAlbumSongs= GetAllSongsFromAlbumID(SelectedAlbumOnAlbumPage.LocalDeviceId!);
            SelectedAlbumOnAlbumPage.IsCurrentlySelected=true;
        }
        else
        {
            AllArtistsAlbumSongs?.Clear();
        }
    }
    
    void SearchArtist(ArtistModelView selectedArtist)
    {
        if (string.IsNullOrEmpty(selectedArtist.Name))
        {
            AllArtists = PlayBackService.GetAllArtists();
            AllArtistsAlbumSongs?.Clear();
            return;
        }

        SelectedArtistOnArtistPage = selectedArtist;
        var song = DisplayedSongs.FirstOrDefault(x => x.ArtistName == SelectedArtistOnArtistPage.Name);
        LoadAllArtistsAlbumsAndLoadAnAlbumSong(song: song, isFromSong: true);
          
    }
    
    [RelayCommand]
    void SearchSongFromArtistAlbumsSongs(string songTitle)
    {
        if (string.IsNullOrWhiteSpace(songTitle))
        {
            SearchArtist(SelectedArtistOnArtistPage);
            return;
        }
        
         AllArtistsAlbumSongs= AllArtistsAlbumSongs
            .Where(x=>x.Title.Contains(songTitle, StringComparison.OrdinalIgnoreCase))
            .ToObservableCollection();
     
    }

    public void ReCheckSongsBelongingToAlbum(string id)
    {
        AllArtistsAlbumSongs?.Clear();
        var alb = AllAlbums.FirstOrDefault(x => x.LocalDeviceId == id);
        
        AllArtistsAlbumSongs= GetAllSongsFromAlbumID(id);
        
        if (alb is not null)
        {
            if (alb.NumberOfTracks != AllArtistsAlbumSongs.Count)
            {
                alb.NumberOfTracks = AllArtistsAlbumSongs.Count;
                alb.TotalDuration = TimeSpan.FromSeconds(AllArtistsAlbumSongs.Sum(x => x.DurationInSeconds)).ToString(@"mm\:ss");
                SongsMgtService.UpdateAlbum(alb);
            }            
        }
       
        //need a linq fxn to go through all this collection and fill this variable
        //PopulateGroupedAlbumSongs(songs.ToList());
        GetTopCompletedPlays(AllArtistsAlbumSongs);
        /*
        TopCompleted = GetTopCompletedPlays(DisplayedSongs);
        TopSkipped = GetTopSkippedPlays(DisplayedSongs);
        OverallStats = GetOverallPlaybackStats(DisplayedSongs);
        RecentPlays = GetRecentlyPlayedSongs(DisplayedSongs, days: 30);
        LeastPlayed = GetLeastPlayedSongs(DisplayedSongs);
        RangeStatss = GetStatsInRange(DisplayedSongs, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        */
    }
   


    /// <summary>
    /// Determines the grouping key based on the first non-whitespace character of the title.
    /// If the first character is not a letter, returns '#'.
    /// </summary>
    /// <param name="title">The title of the song.</param>
    /// <returns>A single character string representing the group name.</returns>
    private string GetGroupingKey(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "#"; // Default group for invalid or empty titles

        // Trim leading whitespace
        string trimmedTitle = title.TrimStart();

        if (string.IsNullOrEmpty(trimmedTitle))
            return "#";

        char firstChar = char.ToUpper(trimmedTitle[0]);

        // Check if the first character is a letter
        return char.IsLetter(firstChar) ? firstChar.ToString() : "#";
    }

    // Existing properties and methods

    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> TopCompleted { get; private set; } = new ObservableCollection<PlaybackStats>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> TopSkipped { get; private set; } = new ObservableCollection<PlaybackStats>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> OverallStats { get; private set; } = new ObservableCollection<PlaybackStats>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> RecentPlays { get; private set; } = new ObservableCollection<PlaybackStats>();
    
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> LeastPlayed { get; private set; } = new ObservableCollection<PlaybackStats>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> RangeStatss { get; private set; } = new ObservableCollection<PlaybackStats>();
    public void GetTopCompletedPlays(IEnumerable<SongModelView> songs)
    {
        TopCompleted = songs.Select(song =>
        {
            var completedPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId && p.PlayType == 3)
                .ToList();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,                
                SongGenre = song.GenreName,
                TotalCompletedPlays = completedPlays.Count,
                //TotalCompletedHours = completedPlays.Sum(p => p.PositionInSeconds) / 3600.0,
                TotalCompletedHours = (song.DurationInSeconds * completedPlays.Count) / 3600.0,
                CompletedPlayTimes = [.. completedPlays.Select(p => p.EventDate)]
            };
        })
        .Where(stat => stat.TotalCompletedPlays > 0) // Only include songs with completed plays
        .OrderByDescending(stat => stat.TotalCompletedPlays)
        .ToObservableCollection();

        
    }
    public void GetTopSkippedPlays(IEnumerable<SongModelView> songs)
    {
        TopSkipped = songs.Select(song =>
        {
            var skippedPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId && p.PlayType == 5) // 5 = Skipped
                .ToObservableCollection();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,
                SongGenre = song.GenreName,
                TotalSkips = skippedPlays.Count,
                SkipTimes = [.. skippedPlays.Select(p => p.EventDate)]
            };
        })
        .Where(stat => stat.TotalSkips > 0) // Only include songs with skips
        .OrderByDescending(stat => stat.TotalSkips)
        .ToObservableCollection();

        
    }
    public ObservableCollection<PlaybackStats> GetOverallPlaybackStats(IEnumerable<SongModelView> songs)
    {
        OverallStats = songs.Select(song =>
        {
            var allPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId)
                .ToObservableCollection();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,
                SongGenre = song.GenreName,
                TotalPlays = allPlays.Count,
                TotalPlayHours = allPlays.Sum(p => p.PositionInSeconds) / 3600.0
            };
        })
        .Where(stat => stat.TotalPlays > 0) // Only include songs with at least one play
        .OrderByDescending(stat => stat.TotalPlays)
        .ToObservableCollection();

        return OverallStats;
    }
    public void GetRecentlyPlayedSongs(IEnumerable<SongModelView> songs, int days = 7)
    {
        var recentDate = DateTime.UtcNow.AddDays(-days);

        RecentPlays = songs.Select(song =>
        {
            var recentPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId && p.EventDate >= recentDate)
                .ToObservableCollection();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,
                SongGenre = song.GenreName,
                TotalPlays = recentPlays.Count,
                TotalPlayHours = recentPlays.Sum(p => p.PositionInSeconds) / 3600.0
            };
        })
        .Where(stat => stat.TotalPlays > 0)
        .OrderByDescending(stat => stat.TotalPlays)
        .ToObservableCollection();

        
    }
    public void GetLeastPlayedSongs(IEnumerable<SongModelView> songs)
    {
        LeastPlayed = GetOverallPlaybackStats(songs)
            .OrderBy(stat => stat.TotalPlays)
            .ToObservableCollection();

        
    }
    public void GetStatsInRange(IEnumerable<SongModelView> songs, DateTime? startDate, DateTime? endDate, bool includeSkips = true, bool includeCompletions = true)
    {
        startDate ??= DateTime.Now.AddDays(-14);
        endDate ??= DateTime.Now;
        var stats = songs.Select(song =>
        {
            var filteredPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId && p.EventDate >= startDate && p.EventDate <= endDate)
                .ToObservableCollection();

            var skippedPlays = includeSkips
                ? filteredPlays.Where(p => p.PlayType == 5).ToObservableCollection()
                : [];

            var completedPlays = includeCompletions
                ? filteredPlays.Where(p => p.WasPlayCompleted).ToObservableCollection()
                : [];

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,
                SongGenre = song.GenreName,
                TotalSkips = skippedPlays.Count,
                SkipTimes = [.. skippedPlays.Select(p => p.EventDate)],
                TotalCompletedPlays = completedPlays.Count,
                CompletedPlayTimes = [.. completedPlays.Select(p => p.EventDate)]
            };
        })
        .Where(stat => stat.TotalSkips > 0 || stat.TotalCompletedPlays > 0)
        .OrderByDescending(stat => stat.TotalCompletedPlays + stat.TotalSkips)
        .ToObservableCollection();

    }

}
