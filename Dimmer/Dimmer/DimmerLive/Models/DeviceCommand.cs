namespace Dimmer.DimmerLive.Models;

[ParseClassName("DeviceCommand")]
public partial class DeviceCommand : ParseObject
{
    [ParseFieldName("targetDeviceId")] public string TargetDeviceId { get => GetProperty<string>(); set => SetProperty(value); }

    [ParseFieldName("senderDeviceId")] public string SenderDeviceId { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("command")] public string Command { get => GetProperty<string>(); set => SetProperty(value); } // PLAY, PAUSE, SKIP, SEEK
    [ParseFieldName("payload")] public string? Payload { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("isProcessed")] public bool IsProcessed { get => GetProperty<bool>(); set => SetProperty(value); }
    [ParseFieldName("senderId")] public string SenderId { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("receiverId")] public string ReceiverId { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("receiverDeviceId")] public string ReceiverDeviceId { get => GetProperty<string>(); set => SetProperty(value); }

}