using DynamicData;


using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace Dimmer.ViewModel;
public partial class SocialViewModel : ObservableObject, IDisposable
{
    private readonly IFriendshipService _friendshipService;
    private readonly IAuthenticationService _authService;

    // UI-Bound Collections
    private readonly ReadOnlyObservableCollection<UserModelOnline> _friends;
    public ReadOnlyObservableCollection<UserModelOnline> Friends => _friends;

    private readonly ReadOnlyObservableCollection<FriendRequest> _friendRequests;
    public ReadOnlyObservableCollection<FriendRequest> FriendRequests => _friendRequests;

    [ObservableProperty]
    public partial ObservableCollection<UserModelOnline> SearchResults { get; set; }

    [ObservableProperty]
    public partial string UserSearchTerm{get;set;}

    [ObservableProperty]
    public partial bool IsBusy{get;set;}

    private readonly CompositeDisposable _disposables = new();

    public SocialViewModel(IFriendshipService friendshipService, IAuthenticationService authService)
    {
        _friendshipService = friendshipService;
        _authService = authService;

        // Bind the service's live data streams to our UI collections
        _friendshipService.Friends
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _friends)
            .Subscribe()
            .DisposeWith(_disposables);

        _friendshipService.IncomingFriendRequests
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _friendRequests)
            .Subscribe()
            .DisposeWith(_disposables);

        // StartAsync listening when a user is logged in
        _authService.CurrentUser
            .Where(user => user != null)
            .Subscribe(_ => _friendshipService.StartListeners())
            .DisposeWith(_disposables);

        // Stop listening when the user logs out
        _authService.CurrentUser
            .Where(user => user == null)
            .Subscribe(_ => _friendshipService.StopListeners())
            .DisposeWith(_disposables);
    }

    [RelayCommand]
    private async Task SearchForUsers()
    {
        if (string.IsNullOrWhiteSpace(UserSearchTerm))
        {
            SearchResults?.Clear();
            return;
        }
        IsBusy = true;
        var users = await _friendshipService.FindUsersAsync(UserSearchTerm);
        SearchResults = new ObservableCollection<UserModelOnline>(users);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task SendFriendRequest(UserModelOnline user)
    {
        IsBusy = true;
        await _friendshipService.SendFriendRequestAsync(user.Username);
        // TODO: Add UI feedback (e.g., a toast notification)
        IsBusy = false;
    }

    [RelayCommand]
    private async Task AcceptRequest(FriendRequest request)
    {
        IsBusy = true;
        await _friendshipService.AcceptFriendRequestAsync(request);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RejectRequest(FriendRequest request)
    {
        IsBusy = true;
        await _friendshipService.RejectFriendRequestAsync(request);
        IsBusy = false;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}