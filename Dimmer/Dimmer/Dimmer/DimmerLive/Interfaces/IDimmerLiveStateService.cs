using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces;
public interface IDimmerLiveStateService
{
    void SaveUserLocally(UserModelView user);
    void SaveUserOnline(UserModelView user);
    
    void TransferUserCurrentDevice(string userId, string originalDeviceId, string newDeviceId);

    void RequestSongFromDifferentDevice(string userId, string songId, string deviceId);

    Task FullySyncUser(string userEmail);
    void DeleteUserLocally(UserModel user);
    Task DeleteUserOnline(ParseUser user);


}
