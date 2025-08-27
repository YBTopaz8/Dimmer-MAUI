using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces;
public record SessionTransferRequest(string FromDeviceName, DimmerSharedSong SongToTransfer);

public interface ISessionTransferService : IDisposable
{
    // An observable stream of incoming transfer requests for this device to handle.
    IObservable<SessionTransferRequest> IncomingTransferRequests { get; }

    // The user on Device A calls this to start the transfer to Device B.
    Task InitiateTransferAsync(UserDeviceSession targetDevice, SongModelView currentSong, double position);

    // The user on Device B calls this after the download is complete to notify Device A.
    Task AcknowledgeTransferCompleteAsync(DimmerSharedSong transferredSong);

    // Lifecycle methods
    void StartListening();
    void StopListeners();
}