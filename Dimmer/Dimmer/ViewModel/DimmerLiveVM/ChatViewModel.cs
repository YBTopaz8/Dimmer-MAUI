using CommunityToolkit.Mvvm.Input;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel.DimmerLiveVM;
public partial class ChatViewModel : ObservableObject,IReactiveObject, IDisposable
{
    public readonly IChatService _chatService;
    private readonly IFriendshipService _friendshipService;
    private readonly CompositeDisposable _disposables = new();

    // --- UI-Bound Collections ---
    private readonly ReadOnlyObservableCollection<ChatConversation> _conversations;
    public ReadOnlyObservableCollection<ChatConversation> Conversations => _conversations;

    private ReadOnlyObservableCollection<ChatMessage> _messages;
    public ReadOnlyObservableCollection<ChatMessage> Messages
    {
        get => _messages;
        private set => this.RaiseAndSetIfChanged(ref _messages, value); // Use ReactiveUI for this
    }

    [ObservableProperty]
    public partial ObservableCollection<UserModelOnline> UserSearchResults { get; set; }

    // --- UI State ---
    [ObservableProperty]
    public partial ChatConversation SelectedConversation{get;set;}
    partial void OnSelectedConversationChanged(ChatConversation oldValue, ChatConversation newValue)
    {
        if (newValue is not null)
        {
            _chatService.GetMessagesForConversation(newValue);

        }
    }

    [ObservableProperty]
    public partial string NewMessageText{get;set;}

    [ObservableProperty]
    public partial string UserSearchTerm{get;set;}

    [ObservableProperty]
    public partial bool IsBusy {get;set;}

    public ChatViewModel(IChatService chatService, IFriendshipService friendshipService)
    {
        _chatService = chatService;
        _friendshipService = friendshipService;

        // Bind the service's conversations to our UI collection
        _chatService.Conversations
            .Sort(SortExpressionComparer<ChatConversation>.Descending(c => c.LastMessageTimestamp))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _conversations)
            .Subscribe()
            .DisposeWith(_disposables);

        // ** This is the key ReactiveUI pattern for dynamic message loading **
        // When the SelectedConversation property changes...
        this.WhenAnyValue(vm => vm.SelectedConversation)
            .Select(convo =>
            {
                // ...if a conversation is selected, get its message stream from the service...
                if (convo == null)
                    return Observable.Empty<IChangeSet<ChatMessage, string>>();
                return _chatService.GetMessagesForConversation(convo);
            })
            .Switch() // ...and switch to that new stream, automatically disposing the old one.
            .Sort(SortExpressionComparer<ChatMessage>.Ascending(m => m.CreatedAt))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _messages) // Bind the result to our Messages property
            .Subscribe()
            .DisposeWith(_disposables);

        // Start chat listeners automatically
        _chatService.StartListeners();
    }

    [RelayCommand]
    private async Task FindAndStartChat(UserModelOnline user)
    {
        if (user == null)
            return;

        IsBusy = true;
        // The service handles all the logic of finding or creating the conversation
        var conversation = await _chatService.GetOrCreateConversationWithUserAsync(user);
        if (conversation != null)
        {
            SelectedConversation = conversation;
        }
        UserSearchTerm = string.Empty; // Clear search after starting a chat
        UserSearchResults?.Clear();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        await _chatService.SendTextMessageAsync(SelectedConversation, NewMessageText);
        NewMessageText = string.Empty; // Clear the input box
    }

    // This method is called by the UI as the user types in the search box
    partial void OnUserSearchTermChanged(string value)
    {
        // Debounce to avoid excessive searching while typing
        Observable.Return(value)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .SelectMany(term => _friendshipService.FindUsersAsync(term))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(results =>
            {
                UserSearchResults = new ObservableCollection<UserModelOnline>(results);
            })
            .DisposeWith(_disposables); // Auto-dispose previous search subscription
    }

    public void Dispose()
    {
        _chatService.StopListeners();
        _disposables.Dispose();
    }

    public void RaisePropertyChanging(System.ComponentModel.PropertyChangingEventArgs args)
    {

        OnPropertyChanging(args);
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        OnPropertyChanged(args);
    }
}