using System.Net.Http;
using System.Windows;

namespace PolEko;

public partial class DeviceInfoDisplay
{
  private readonly Device _device;
  public DeviceInfoDisplay(Device device)
  {
    _device = device;
    InitializeComponent();
    NameBlock.Text = device.ToString();
    IpBlock.Text = device.IpAddress.ToString();
    TypeBlock.Text = device.Type;
    DescriptionBlock.Text = device.Description;
    RefreshRateBlock.Text = device.RefreshRate.ToString();
  }

  private async void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    HttpClient client = new();
    var dev = (WeatherDevice)_device;
    var measurement = await dev.GetMeasurement(client);
    MessageBox.Show(measurement.ToString());
  }
}