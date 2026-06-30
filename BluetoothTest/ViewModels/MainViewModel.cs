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
        private const string KeyLastDeviceAddress = "last_device_address";

        private readonly IBluetoothService _bluetooth;
        private readonly ISettingsService  _settings;
        private CancellationTokenSource?   _reconnectCts;

        public ObservableCollection<BluetoothDeviceInfo> Devices { get; } = new();

        [ObservableProperty] private BluetoothDeviceInfo? selectedDevice;
        [ObservableProperty] private string?              lastError = "";
        [ObservableProperty] private bool                 isConnected;
        [ObservableProperty] private bool?                lightState;
        [ObservableProperty] private bool                 isDevicePickerOpen;

        // nome mostrato nell'header ("Nessun dispositivo" se non ancora scelto)
        [ObservableProperty] private string targetDeviceName = "Nessun dispositivo";

        public bool LightIsOn  => LightState == true;
        public bool LightIsOff => LightState == false;

        partial void OnLightStateChanged(bool? value)
        {
            OnPropertyChanged(nameof(LightIsOn));
            OnPropertyChanged(nameof(LightIsOff));
        }

        public MainViewModel(IBluetoothService bluetoothService, ISettingsService settingsService)
        {
            _bluetooth = bluetoothService;
            _settings  = settingsService;

            _bluetooth.Disconnected += OnBluetoothDisconnected;
            _bluetooth.DataReceived += OnDataReceived;

            LoadDevices();
        }

        // ── Gestione dispositivo target ──────────────────────────────────────

        [RelayCommand]
        public void ToggleDevicePicker() => IsDevicePickerOpen = !IsDevicePickerOpen;

        [RelayCommand]
        public async Task SelectDevice(BluetoothDeviceInfo device)
        {
            SelectedDevice     = device;
            TargetDeviceName   = device.Name;
            IsDevicePickerOpen = false;

            _settings.Set(KeyLastDeviceAddress, device.Address);

            // disconnette l'eventuale sessione corrente e si riconnette al nuovo dispositivo
            if (IsConnected)
                await _bluetooth.DisconnectAsync();

            await Connect();
        }

        // ── Comandi principali ───────────────────────────────────────────────

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

                // prova a trovare l'ultimo dispositivo usato
                var savedAddress = _settings.Get(KeyLastDeviceAddress);
                var target = savedAddress != null
                    ? Devices.FirstOrDefault(d => d.Address == savedAddress)
                    : null;

                if (target != null)
                {
                    SelectedDevice   = target;
                    TargetDeviceName = target.Name;
                    await Connect();
                }
                else
                {
                    // nessun dispositivo salvato: apre il picker
                    IsDevicePickerOpen = true;
                    TargetDeviceName   = "Nessun dispositivo";
                }
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
                    LightState = null;
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
            catch (Exception ex) { LastError = ex.Message; }
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
            catch (Exception ex) { LastError = ex.Message; }
        }

        // ── Ricezione dati ───────────────────────────────────────────────────

        private void OnDataReceived(object? sender, byte[] data)
        {
            var msg = Encoding.UTF8.GetString(data).Trim();
            Dispatcher.UIThread.Post(() =>
            {
                if      (msg.Contains("OK:1")) { LightState = true;  LastError = ""; }
                else if (msg.Contains("OK:0")) { LightState = false; LastError = ""; }
            });
        }

        // ── Auto-riconnessione ───────────────────────────────────────────────

        private void OnBluetoothDisconnected(object? sender, EventArgs e)
        {
            IsConnected = false;
            LightState  = null;
            LastError   = "Dispositivo disconnesso.";
            StartAutoReconnect();
        }

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
                    await Connect();
                });

                if (IsConnected) return;
            }
        }
    }
}
