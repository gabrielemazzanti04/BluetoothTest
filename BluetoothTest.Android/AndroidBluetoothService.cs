using Android.Bluetooth;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BluetoothTest.Android
{
    public class AndroidBluetoothService : IBluetoothService
    {
        private BluetoothSocket? _socket;
        private CancellationTokenSource? _readCts;

        public bool IsConnected => _socket?.IsConnected ?? false;

        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler? Disconnected;

        public async Task<bool> ConnectAsync(string address)
        {
            var manager = (BluetoothManager)Application.Context
                .GetSystemService(Context.BluetoothService)!;

            var adapter = manager.Adapter;
            adapter.CancelDiscovery();

            var device = adapter.GetRemoteDevice(address);
            var uuid = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");

            _socket = device.CreateRfcommSocketToServiceRecord(uuid);
            await _socket.ConnectAsync();

            if (!_socket.IsConnected)
                return false;

            _readCts = new CancellationTokenSource();
            _ = Task.Run(() => ReadLoopAsync(_readCts.Token));
            return true;
        }

        public async Task SendAsync(byte[] data)
        {
            if (_socket == null || !_socket.IsConnected)
                return;

            await _socket.OutputStream!.WriteAsync(data, 0, data.Length);
        }

        public Task DisconnectAsync()
        {
            CloseSocket();
            return Task.CompletedTask;
        }

        private void CloseSocket()
        {
            _readCts?.Cancel();
            _readCts = null;

            var s = Interlocked.Exchange(ref _socket, null);
            if (s != null)
            {
                try { s.Close(); } catch { }
                s.Dispose();
            }
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[1024];

            try
            {
                while (!ct.IsCancellationRequested && _socket?.IsConnected == true)
                {
                    var read = await _socket.InputStream!.ReadAsync(buffer, 0, buffer.Length, ct);

                    if (read > 0)
                    {
                        var data = new byte[read];
                        Array.Copy(buffer, data, read);
                        DataReceived?.Invoke(this, data);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch
            {
                // connessione caduta inaspettatamente
            }
            finally
            {
                // se il loop termina senza che sia stata chiamata DisconnectAsync,
                // è una disconnessione improvvisa: puliamo e notifichiamo la UI
                if (_socket != null)
                {
                    CloseSocket();
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync()
        {
            var manager = (BluetoothManager)Application.Context
                .GetSystemService(Context.BluetoothService)!;

            var adapter = manager.Adapter;

            IReadOnlyList<BluetoothDeviceInfo> result = adapter.BondedDevices!
                .Select(d => new BluetoothDeviceInfo
                {
                    Name = d.Name ?? "Unknown",
                    Address = d.Address ?? ""
                })
                .ToList();

            return Task.FromResult(result);
        }
    }
}
