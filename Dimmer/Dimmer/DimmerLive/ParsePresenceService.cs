using DynamicData;

using Parse.LiveQuery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive;
public class ParsePresenceService : IPresenceService
{
    private readonly IAuthenticationService _authService;
    private readonly ParseLiveQueryClient _liveQueryClient;
    private readonly ILogger<ParsePresenceService> _logger;
    private UserDeviceSession? _myDeviceSession;
    private readonly SourceCache<UserDeviceSession, string> _otherDevicesCache = new(d => d.ObjectId);
    private Subscription<UserDeviceSession>? _stateSubscription;
    private System.Timers.Timer? _presenceTimer;

    public IObservable<IChangeSet<UserDeviceSession, string>> OtherActiveDevices => _otherDevicesCache.Connect();

    public ParsePresenceService(IAuthenticationService authService, ParseLiveQueryClient liveQueryClient, ILogger<ParsePresenceService> logger)
    {
        _authService = authService;
        _liveQueryClient = liveQueryClient;
        _logger = logger;
    }

    public async Task AnnouncePresenceAsync()
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null)
            return;

        var parameters = new Dictionary<string, object>
        {
            { "deviceId", Preferences.Get("MyDeviceId", Guid.NewGuid().ToString()) },
            { "deviceName", DeviceInfo.Name },
            { "deviceIdiom", DeviceInfo.Idiom.ToString() },
            { "deviceOSVersion", DeviceInfo.VersionString }
        };

        try
        {
            // Call the cloud function to handle creating/updating the session object.
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<UserDeviceSession>("registerDevicePresence", parameters);
            _myDeviceSession = result;
            _logger.LogInformation("Successfully registered device presence. Session ID: {SessionId}", _myDeviceSession.ObjectId);
            StartPresenceTimer();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device presence.");
        }
    }

    public async Task GoOfflineAsync()
    {
        _presenceTimer?.Stop();
        if (_myDeviceSession != null)
        {
            var parameters = new Dictionary<string, object> { { "sessionObjectId", _myDeviceSession.ObjectId } };
            try
            {
                await ParseClient.Instance.CallCloudCodeFunctionAsync<string>("markDeviceInactive", parameters);
                _logger.LogInformation("Successfully marked device as inactive.");
                _myDeviceSession = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark device as inactive.");
            }
        }
    }

    public void StartListeners()
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null)
            return;

        var stateQuery = new ParseQuery<UserDeviceSession>(ParseClient.Instance)
            .WhereEqualTo("userOwner", currentUser)
            .WhereNotEqualTo("deviceId", _myDeviceSession?.DeviceId) // Exclude our own device
            .WhereEqualTo("isActive", true);

        _stateSubscription = _liveQueryClient.Subscribe(stateQuery);
        _stateSubscription.On(Subscription.Event.Enter, device => _otherDevicesCache.AddOrUpdate(device));
        _stateSubscription.On(Subscription.Event.Create, device => _otherDevicesCache.AddOrUpdate(device));
        _stateSubscription.On(Subscription.Event.Update, device => _otherDevicesCache.AddOrUpdate(device));
        _stateSubscription.On(Subscription.Event.Leave, device => _otherDevicesCache.Remove(device));
    }

    public void StopListeners() => _stateSubscription?.UnsubscribeNow();
    
    private void StartPresenceTimer()
    {
        _presenceTimer?.Stop();
        _presenceTimer = new System.Timers.Timer(TimeSpan.FromMinutes(2).TotalMilliseconds);
        _presenceTimer.Elapsed += async (s, e) => await AnnouncePresenceAsync(); // Re-announce every 2 minutes
        _presenceTimer.AutoReset = true;
        _presenceTimer.Start();
    }

    public void Dispose()
    {
        _presenceTimer?.Dispose();
        StopListeners();
        _otherDevicesCache.Dispose();
    }
}