using System.Collections.Generic;
using System.Net;
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

  private void AddDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new(ProcessNewDevice);
    prompt.Show();
  }

  private void ProcessNewDevice(IPAddress ipAddress)
  {
    Device device = new(ipAddress);
    Devices.Add(device);
    DevicesBox.Items.Add(device.IpAddress.ToString());
  }
}