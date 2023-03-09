using System;
using System.Collections.Generic;
using System.Net;
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

  public MainWindow()
  {
    InitializeComponent();
    DevicesBox.SelectionChanged += HandleDeviceChange;
  }

  private List<Device> Devices { get; } = new();

  private void HandleDeviceChange(object sender, SelectionChangedEventArgs e)
  {
    if (_deviceInfo != null) Grid.Children.Remove(_deviceInfo);
    
    if (sender is not ComboBox { SelectedValue: Device value }) return;
    _currentDevice = value;

    _deviceInfo = new(_currentDevice);
    Grid.SetRow(_deviceInfo, 1);
    Grid.Children.Add(_deviceInfo);
  }

  private void AddDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new(AddNewDevice);
    prompt.Show();
  }

  private void AddNewDevice(IPAddress ipAddress, int port, string? id)
  {
    WeatherDevice weatherDevice = new(ipAddress, port, id);
    if (Devices.Contains(weatherDevice))
    {
      MessageBox.Show("Urządzenie już istnieje");
      return;
    }

    Devices.Add(weatherDevice);
    DevicesBox.Items.Add(weatherDevice);
  }

  private void FetchMeasurements_Click(object sender, RoutedEventArgs e)
  {
    throw new NotImplementedException();
  }
}