using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BluetoothTest.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        // Nome esatto del dispositivo a cui connettersi automaticamente (lascia vuoto per disabilitare)
        private const string AutoConnectDeviceName = "ESP32-Luce";

        private readonly IBluetoothService _bluetooth;
        public ObservableCollection<BluetoothDeviceInfo> Devices { get; } = new();

        [ObservableProperty]
        private BluetoothDeviceInfo? selectedDevice;
        [ObservableProperty]
        private string? lastError = "";

        [ObservableProperty]
        private bool isConnected;

        public MainViewModel(IBluetoothService bluetoothService)
        {
            _bluetooth = bluetoothService;
            _bluetooth.Disconnected += OnBluetoothDisconnected;
        }

        private void OnBluetoothDisconnected(object? sender, EventArgs e)
        {
            IsConnected = false;
            LastError = "Dispositivo disconnesso.";
        }

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
                if (SelectedDevice == null)
                    return;

                IsConnected = await _bluetooth.ConnectAsync(SelectedDevice.Address);
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
                LastError = "";
                await _bluetooth.DisconnectAsync();
                IsConnected = false;
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
    }
}
