using System.Net;
using System.Windows;

namespace PolEko;

/// <summary>
///   Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
  public MainWindow()
  {
    InitializeComponent();
  }

  private void AddDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new(ProcessNewDevice);
    prompt.Show();
  }

  private static void ProcessNewDevice(IPAddress ipAddress)
  {
    Device device = new(ipAddress);
    MessageBox.Show($"{device.IpAddress}");
  }
}