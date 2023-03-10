using System;
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
  private DeviceInfoDisplay? _deviceInfo;
  private HttpClient? _httpClient;

  public MainWindow()
  {
    InitializeComponent();
    SideMenu sideMenu = new(Devices, AddNewDevice, HandleDeviceChange);
    sideMenu.Margin = new Thickness(5);
    Grid.Children.Add(sideMenu);
  }

  private ObservableCollection<Device> Devices { get; } = new();

  private void HandleDeviceChange(object sender, RoutedEventArgs e)
  {
    if (_deviceInfo != null) Grid.Children.Remove(_deviceInfo);
    
    if (sender is not Button value) throw new ArgumentException("You can only use this method to handle Button Click event");
    if (value.Content is not Device curr) throw new ArgumentException("Button's content can only be of type Device");
    
    _currentDevice = curr;
    var httpClient = _httpClient ??= new HttpClient();
    _deviceInfo = new(_currentDevice, httpClient);
    Grid.Children.Add(_deviceInfo);
    Grid.SetColumn(_deviceInfo, 1);
  }

  private void AddNewDevice(IPAddress ipAddress, ushort port, string? id)
  {
    WeatherDevice weatherDevice = new(ipAddress, port, id);
    if (Devices.Contains(weatherDevice))
    {
      MessageBox.Show("Urządzenie już istnieje");
      return;
    }

    Devices.Add(weatherDevice);
  }
}