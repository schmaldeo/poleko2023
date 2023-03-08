using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace PolEko;

/// <summary>
///   Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
  private List<Device> Devices { get; } = new();
  public MainWindow()
  {
    InitializeComponent();
  }
  
  // On main windows closing, dont close instantly, rather send potential leftover buffer to database
  // Database calls should be happening every whatever items in the buffer (Queue<> ?)
  
  private void AddDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new(AddNewDevice);
    prompt.Show();
  }

  private void AddNewDevice(IPAddress ipAddress, int port)
  {
    WeatherDevice weatherDevice = new(ipAddress, port);
    Devices.Add(weatherDevice);
    DevicesBox.Items.Add(weatherDevice);
  }

  private void FetchMeasurements_Click(object sender, RoutedEventArgs e)
  {
    DevicesBox.SelectedValue.ToString();
    MessageBox.Show("lol");
  }
}