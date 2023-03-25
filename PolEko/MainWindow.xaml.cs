using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
  private HttpClient? _httpClient;
  private readonly ObservableCollection<Device> _devices = new();

  public MainWindow(IEnumerable<Device>? devices = null)
  {
    InitializeComponent();
    SideMenu sideMenu = new(_devices, AddNewDevice, HandleDeviceChange)
    {
      Margin = new Thickness(5)
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
    
    // If some device is currently displayed, dispose its timer and remove it from view
    _deviceInfo?.Dispose();
    if (_deviceInfo is not null) Grid.Children.Remove(_deviceInfo);
    
    _currentDevice = incomingDevice;
    var httpClient = _httpClient ??= new HttpClient();
    // TODO: Reflection
    if (_currentDevice is SmartProDevice device)
    {
      _deviceInfo = new(device, httpClient, RemoveDevice);
      Closing += device.HandleBufferOverflow;
    }
    Grid.Children.Add(_deviceInfo);
    Grid.SetColumn(_deviceInfo, 1);
  }
  
  // TODO: reflection
  private async void AddNewDevice(IPAddress ipAddress, ushort port, string? id)
  {
    SmartProDevice smartProDevice = new(ipAddress, port, id);
    if (_devices.Contains(smartProDevice))
    {
      MessageBox.Show("Urządzenie już istnieje");
      return;
    }

    await Database.AddDeviceAsync(smartProDevice, typeof(SmartProDevice));
    _devices.Add(smartProDevice);
  }
  
  private async void RemoveDevice(Device device)
  {
    await Database.RemoveDeviceAsync(device);
    Grid.Children.Remove(_deviceInfo);
    _devices.Remove(device);
  }
}