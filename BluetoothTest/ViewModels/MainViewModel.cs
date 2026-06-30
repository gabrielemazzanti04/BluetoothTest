using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BluetoothTest.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        // Nome esatto del dispositivo a cui connettersi automaticamente (lascia vuoto per disabilitare)
        private const string AutoConnectDeviceName = "ESP32-Luce";

        private readonly IBluetoothService _bluetooth;
        private CancellationTokenSource? _reconnectCts;

        public ObservableCollection<BluetoothDeviceInfo> Devices { get; } = new();

        [ObservableProperty] private BluetoothDeviceInfo? selectedDevice;
        [ObservableProperty] private string? lastError = "";
        [ObservableProperty] private bool isConnected;

        // true = accesa, false = spenta, null = stato sconosciuto
        [ObservableProperty] private bool? lightState;

        public bool LightIsOn  => LightState == true;
        public bool LightIsOff => LightState == false;

        partial void OnLightStateChanged(bool? value)
        {
            OnPropertyChanged(nameof(LightIsOn));
            OnPropertyChanged(nameof(LightIsOff));
        }

        public MainViewModel(IBluetoothService bluetoothService)
        {
            _bluetooth = bluetoothService;
            _bluetooth.Disconnected   += OnBluetoothDisconnected;
            _bluetooth.DataReceived   += OnDataReceived;
            LoadDevices();
        }

        private void OnBluetoothDisconnected(object? sender, EventArgs e)
        {
            IsConnected = false;
            LightState  = null;
            LastError   = "Dispositivo disconnesso.";
            StartAutoReconnect();
        }

        private void OnDataReceived(object? sender, byte[] data)
        {
            // L'ESP32 risponde "OK:0\n" o "OK:1\n"
            var msg = Encoding.UTF8.GetString(data).Trim();
            Dispatcher.UIThread.Post(() =>
            {
                if (msg.Contains("OK:1"))      { LightState = true;  LastError = ""; }
                else if (msg.Contains("OK:0")) { LightState = false; LastError = ""; }
            });
        }

        // ── Comandi ─────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task LoadDevices()
        {
            try
            {
                LastError = "";
                Devices.Clear();

                var devices = await _bluetooth.GetPairedDevicesAsync();
                foreach (var d in devices)
                    Devices.Add(d);

                var target = !string.IsNullOrEmpty(AutoConnectDeviceName)
                    ? Devices.FirstOrDefault(d => d.Name.Contains(AutoConnectDeviceName, StringComparison.OrdinalIgnoreCase))
                    : null;

                SelectedDevice = target ?? Devices.FirstOrDefault();

                if (target != null)
                    await Connect();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        [RelayCommand]
        public async Task Connect()
        {
            try
            {
                LastError = "";
                if (SelectedDevice == null) return;

                IsConnected = await _bluetooth.ConnectAsync(SelectedDevice.Address);

                if (IsConnected)
                {
                    _reconnectCts?.Cancel();
                    LightState = null; // in attesa della risposta di stato dall'ESP32
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        [RelayCommand]
        public async Task Disconnect()
        {
            try
            {
                _reconnectCts?.Cancel();
                LastError = "";
                await _bluetooth.DisconnectAsync();
                IsConnected = false;
                LightState  = null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        [RelayCommand]
        public async Task SendOne()
        {
            try
            {
                LastError = "";
                if (!IsConnected) return;
                await _bluetooth.SendAsync(new byte[] { (byte)'1' });
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        [RelayCommand]
        public async Task SendZero()
        {
            try
            {
                LastError = "";
                if (!IsConnected) return;
                await _bluetooth.SendAsync(new byte[] { (byte)'0' });
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        // ── Auto-riconnessione ───────────────────────────────────────────────

        private void StartAutoReconnect()
        {
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();
            _ = AutoReconnectLoopAsync(_reconnectCts.Token);
        }

        private async Task AutoReconnectLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(4000, ct); } catch (OperationCanceledException) { return; }

                if (IsConnected) return;

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    LastError = "Riconnessione in corso...";
                    await LoadDevices();
                });

                if (IsConnected) return;
            }
        }
    }
}
