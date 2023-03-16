using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace PolEko;

/// <summary>
///   Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
  private Device? _currentDevice;
  private DeviceInfoControl? _deviceInfo;
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
    if (sender is not Button value) throw new ArgumentException("You can only use this method to handle Button Click event");
    if (value.Content is not Device incomingDevice) throw new ArgumentException("Button's content can only be of type Device");

    // Disallow reopening a device that's currently open
    if (_currentDevice is not null && _currentDevice.Equals(incomingDevice)) return;
    
    // If some device is currently displayed, dispose its timer and remove it from view
    _deviceInfo?.Dispose();
    if (_deviceInfo is not null) Grid.Children.Remove(_deviceInfo);
    
    _currentDevice = incomingDevice;
    var httpClient = _httpClient ??= new HttpClient();
    _deviceInfo = new(_currentDevice, httpClient, EditDevice);
    Grid.Children.Add(_deviceInfo);
    Grid.SetColumn(_deviceInfo, 1);
  }

  private async void AddNewDevice(IPAddress ipAddress, ushort port, string? id)
  {
    WeatherDevice weatherDevice = new(ipAddress, port, id);
    if (_devices.Contains(weatherDevice))
    {
      MessageBox.Show("Urządzenie już istnieje");
      return;
    }
    
    await using var connection = new SqliteConnection("Data Source=Measurements.db");
    await connection.OpenAsync();
    
    await Database.AddDevice(connection, weatherDevice, typeof(WeatherDevice));
    _devices.Add(weatherDevice);
  }
  
  private void EditDevice(IPAddress ipAddress, ushort port, string? id)
  {
    // TODO: figure out if checking for changes is quicker than just potentially overwriting with the same value
    // causing _toString cache to reset
    if (_currentDevice is null) return;
    
    _currentDevice.IpAddress = ipAddress;
    _currentDevice.Port = port;
    _currentDevice.Id = id;
  }
}