using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Interfaces.Services;

public partial class AppInitializerService : IAppInitializerService
{
    private readonly IDimmerStateService _state;
    private readonly IMapper _mapper;
    private readonly IRepository<UserModel> _userRepo;
    private readonly IRepository<AppStateModel> _appStateRepo;
    private readonly ILibraryScannerService _libraryScanner;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AppInitializerService> _logger;

    public AppInitializerService(
        IDimmerStateService state,
        IMapper mapper,
        IRepository<UserModel> userRepo,
        IRepository<AppStateModel> appStateRepo,
        ILibraryScannerService libraryScanner,
        ISettingsService settingsService,
        ILogger<AppInitializerService> logger)
    {
        _state = state;
        _mapper = mapper;
        _userRepo = userRepo;
        _appStateRepo = appStateRepo;
        _libraryScanner = libraryScanner;
        _settingsService = settingsService;
        _logger = logger;
    }

    public void InitializeApplication()
    {

        _logger.LogInformation("Application Initializer: Starting initialization...");
        try
        {
            // 1. Load/Initialize User
            var users = _userRepo.GetAll(); // Assuming async repo
            UserModel? currentUserInstance = users.FirstOrDefault();
            if (currentUserInstance == null)
            {
                currentUserInstance = new UserModel { UserName = "Default User" /* other defaults */ };
                currentUserInstance = _userRepo.Upsert(currentUserInstance);
                _logger.LogInformation("No existing user. Created default user.");
            }
            _state.SetCurrentUser(_mapper.Map<UserModelView>(currentUserInstance));

            // 3. Set initial playback defaults in state from settings
            _state.SetShuffleActive(_settingsService.ShuffleOn);
            _state.SetRepeatMode(_settingsService.RepeatMode);
            _state.SetDeviceVolume(_settingsService.LastVolume); // Initialize global state volume
            _logger.LogInformation("Initial playback settings (shuffle, repeat, volume) set in global state.");

           

            _logger.LogInformation("Application Initializer: Initialization complete.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error during application initialization.");
            throw; // Rethrow to indicate startup failure
        }
    }

    public Task LoadApplicationStateAsync()
    {
        throw new NotImplementedException();
    }

    public Task SaveApplicationStateAsync(AppStateModelView appStateView)
    {
        throw new NotImplementedException();
    }
}