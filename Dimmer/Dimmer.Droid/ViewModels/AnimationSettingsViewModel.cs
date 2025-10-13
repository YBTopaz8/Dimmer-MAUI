using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.LastFM;

using Microsoft.Extensions.Logging;

using System.Reflection;

namespace Dimmer.ViewModels;
public class AnimationSettingsViewModel : BaseViewModelAnd
{
 
    private Type _selectedPageType;
    private AnimationSetting _selectedPushEnter;
    private AnimationSetting _selectedPushExit;
    private AnimationSetting _selectedPopEnter;
    private AnimationSetting _selectedPopExit;
    private bool _isLoading;
    private readonly IAnimationService _animationService;

    public AnimationSettingsViewModel(IDimmerAudioService AudioService,
        IAnimationService animationService,
        ILogger<BaseViewModelAnd> logger, IMapper mapper, IDimmerStateService dimmerStateService, MusicDataService musicDataService, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> SongRepo, IDuplicateFinderService duplicateFinderService, ILastfmService LastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService) : base(AudioService, logger, mapper, dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, SongRepo, duplicateFinderService, LastfmService, artistRepo, albumModel, genreModel, dialogueService)
    {   


        _animationService = animationService;

        // 1. Load all available animations ONCE.
        AvailableAnimations = _animationService.GetAvailableAnimations();

        // 2. Use Reflection to find all pages in the app.
        AppPages = Assembly.GetExecutingAssembly().GetTypes()
                         .Where(t => t.IsSubclassOf(typeof(ContentPage)) && !t.IsAbstract)
                         .OrderBy(t => t.Name)
                         .ToList();

        // 3. Pre-select the first page to avoid an empty UI.
        if (AppPages.Any())
        {
            SelectedPageType = AppPages.First();
        }
    }


    public List<AnimationSetting> AvailableAnimations { get; }
    public List<Type> AppPages { get; }

    public Type SelectedPageType
    {
        get => _selectedPageType;
        set
        {
            if (SetProperty(ref _selectedPageType, value))
            {
                // When the user selects a different page, load its animations.
                LoadAnimationsForSelectedPage();
            }
        }
    }

    public AnimationSetting SelectedPushEnter
    {
        get => _selectedPushEnter;
        set
        {
            if (SetProperty(ref _selectedPushEnter, value))
            {
                SaveAnimationsForSelectedPage();
            }
        }
    }
    // (Repeat for the other 3 animation properties)
    public AnimationSetting SelectedPushExit { get { return _selectedPushExit; } set { if (SetProperty(ref _selectedPushExit, value)) SaveAnimationsForSelectedPage(); } }
    public AnimationSetting SelectedPopEnter { get => _selectedPopEnter; set { if (SetProperty(ref _selectedPopEnter, value)) SaveAnimationsForSelectedPage(); } }
    public AnimationSetting SelectedPopExit { get => _selectedPopExit; set { if (SetProperty(ref _selectedPopExit, value)) SaveAnimationsForSelectedPage(); } }


    private void LoadAnimationsForSelectedPage()
    {
        if (SelectedPageType == null)
            return;

        // A flag to prevent the Save method from triggering while we are just loading data.
        _isLoading = true;

        var profile = AnimationManager.GetPageAnimations(SelectedPageType, _animationService);

        SelectedPushEnter = AvailableAnimations.FirstOrDefault(a => a.ResourceId == profile.PushEnter.ResourceId);
        SelectedPushExit = AvailableAnimations.FirstOrDefault(a => a.ResourceId == profile.PushExit.ResourceId);
        SelectedPopEnter = AvailableAnimations.FirstOrDefault(a => a.ResourceId == profile.PopEnter.ResourceId);
        SelectedPopExit = AvailableAnimations.FirstOrDefault(a => a.ResourceId == profile.PopExit.ResourceId);

        _isLoading = false;
    }

    private void SaveAnimationsForSelectedPage()
    {
        // Don't save if we are in the middle of loading or if nothing is selected.
        if (_isLoading || SelectedPageType == null)
            return;

        AnimationManager.SetPageAnimations(
            SelectedPageType,
            SelectedPushEnter,
            SelectedPushExit,
            SelectedPopEnter,
            SelectedPopExit
        );
    }
}
