using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothTest
{
    public interface IBluetoothService
    {
        Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync();

        Task<bool> ConnectAsync(string address);

        Task DisconnectAsync();

        Task SendAsync(byte[] data);

        bool IsConnected { get; }
    }
}
