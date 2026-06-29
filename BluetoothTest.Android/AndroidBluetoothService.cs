using Android.Bluetooth;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothTest.Android
{


    public class AndroidBluetoothService : IBluetoothService
    {
        private BluetoothSocket? _socket;

        public bool IsConnected => _socket?.IsConnected ?? false;

        public event EventHandler<byte[]>? DataReceived;

        public async Task<bool> ConnectAsync(string address)
        {
            var manager = (BluetoothManager)Application.Context
                .GetSystemService(Context.BluetoothService)!;

            var adapter = manager.Adapter;

            adapter.CancelDiscovery();

            var device = adapter.GetRemoteDevice(address);

            var uuid = Java.Util.UUID.FromString(
                "00001101-0000-1000-8000-00805F9B34FB");

            _socket = device.CreateRfcommSocketToServiceRecord(uuid);

            await _socket.ConnectAsync();

            if (!_socket.IsConnected)
                return false;

            StartReading();
            return true;
        }

        public async Task SendAsync(byte[] data)
        {
            if (_socket == null)
                return;

            await _socket.OutputStream.WriteAsync(data, 0, data.Length);
        }

        public async Task DisconnectAsync()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }
        }

        private async void StartReading()
        {
            var buffer = new byte[1024];

            while (_socket?.IsConnected == true)
            {
                var read = await _socket.InputStream.ReadAsync(buffer);

                if (read > 0)
                {
                    var data = new byte[read];
                    Array.Copy(buffer, data, read);

                    DataReceived?.Invoke(this, data);
                }
            }
        }

        public async Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync()
        {
            var manager = (BluetoothManager)Application.Context
                .GetSystemService(Context.BluetoothService)!;

            var adapter = manager.Adapter;

            return adapter.BondedDevices
                .Select(d => new BluetoothDeviceInfo
                {
                    Name = d.Name ?? "Unknown",
                    Address = d.Address
                })
                .ToList();
        }
    }
}
