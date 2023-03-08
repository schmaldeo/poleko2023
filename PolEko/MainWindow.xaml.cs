using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PolEko;

/// <summary>
///   Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
  private List<Device> Devices { get; } = new();
  private Device? _currentDevice;

  public MainWindow()
  {
    InitializeComponent();
    DevicesBox.SelectionChanged += HandleDeviceChange;
  }
  
  private void HandleDeviceChange(object sender, SelectionChangedEventArgs e)
  {
    if (sender is not ComboBox) return;
    var devicesBox = (ComboBox)sender;
    
    if (devicesBox.SelectedValue is not Device) return;
    _currentDevice = (Device)devicesBox.SelectedValue;
  } 
  
  // On main windows closing, dont close instantly, rather send potential leftover buffer to database
  // Database calls should be happening every whatever items in the buffer (Queue<> ?)
  
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