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
            LoadDevices();
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
                SelectedDevice = Devices.FirstOrDefault();
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
