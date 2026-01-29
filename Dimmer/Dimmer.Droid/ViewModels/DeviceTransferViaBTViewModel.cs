using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dimmer.DimmerLive;

namespace Dimmer.ViewModels;

public partial class DeviceTransferViaBTViewModel :ObservableObject
{

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsTransferInProgress { get; set; }
    public BaseViewModelAnd MyViewModel { get; }

    private readonly IBluetoothService bluetoothService;

    public DeviceTransferViaBTViewModel(BaseViewModelAnd vm, IBluetoothService bluetoothService)
    {
        MyViewModel = vm;
        this.bluetoothService = bluetoothService;
    }

    public async Task StartBluetoothServerAsync()
    {
        bluetoothService.StatusChanged += BluetoothService_StatusChanged;

        await bluetoothService.StartServerAsync();
        await bluetoothService.GetPairedDevicesAsync();
    }

    public async Task ConnectToDevice(string devName)
    {
        await bluetoothService.ConnectToDeviceAsync(devName);
    }

    private void BluetoothService_StatusChanged(object? sender, string e)
    {
        StatusMessage = e;

    }

}
