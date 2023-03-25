using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace PolEko;

/// <summary>
///   Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
  private Device? _currentDevice;
  private WeatherDeviceInfoControl? _deviceInfo;
  private List<TabItem> _openDevices = new();
  private HttpClient? _httpClient;
  private readonly ObservableCollection<Device> _devices = new();

  public MainWindow(Dictionary<string, Type> types, IEnumerable<Device>? devices = null)
  {
    InitializeComponent();
    SideMenu sideMenu = new(_devices, AddNewDevice, HandleDeviceChange, types)
    {
      Margin = new Thickness(5),
    };
    if (devices is not null)
    {
      foreach (var device in devices)
      {
        _devices.Add(device);
      }
    }
    Grid.Children.Add(sideMenu);
  }


  private void HandleDeviceChange(object sender, RoutedEventArgs e)
  {
    // Check for potential invalid args
    if (sender is not ListBoxItem value) throw new ArgumentException("You can only use this method to handle ListBoxItem Click event");
    if (value.Content is not Device incomingDevice) throw new ArgumentException("Button's content can only be of type Device");

    // Disallow reopening a device that's currently open
    if (_currentDevice is not null && _currentDevice.Equals(incomingDevice)) return;

    _currentDevice = incomingDevice;
    var httpClient = _httpClient ??= new HttpClient();
    // TODO: Reflection
    if (_currentDevice is SmartProDevice device)
    {
      _deviceInfo = new(device, httpClient, RemoveDevice);
      Closing += device.HandleBufferOverflow;
    }

    if (_currentDevice is ExampleDevice)
    {
      MessageBox.Show("Unimplemented");
      return;
    }

    var item = new TabItem
    {
      Content = _deviceInfo,
      Header = $"{_currentDevice.IpAddress}:{_currentDevice.Port}"
    };
    if (_openDevices.Exists(x => (string)x.Header == $"{_currentDevice.IpAddress}:{_currentDevice.Port}")) return;
    _openDevices.Add(item);
    TabControl.Items.Add(item);
    TabControl.SelectedItem = item;
  }
  
  private async void AddNewDevice(IPAddress ipAddress, ushort port, string? id, Type type)
  {
    var device = (Device)Activator.CreateInstance(type, ipAddress, port, id);
    // TODO: figure out why this doesn't trigger when the type is different but ip and port the same
    if (_devices.Contains(device))
    {
      MessageBox.Show("Urządzenie już istnieje");
      return;
    }

    await Database.AddDeviceAsync(device, type);
    _devices.Add(device);
  }
  
  private async void RemoveDevice(Device device)
  {
    await Database.RemoveDeviceAsync(device);
    Grid.Children.Remove(_deviceInfo);
    _devices.Remove(device);
  }
}