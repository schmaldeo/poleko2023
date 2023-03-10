using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Windows;

namespace PolEko;

public partial class DeviceInfoDisplay
{
  private HttpClient _client;
  private readonly Device _device;
  public DeviceInfoDisplay(Device device, HttpClient client)
  {
    _device = device;
    _client = client;
    InitializeComponent();
    NameBlock.Text = device.ToString();
    IpBlock.Text = device.IpAddress.ToString();
    TypeBlock.Text = device.Type;
    DescriptionBlock.Text = device.Description;
    RefreshRateBlock.Text = device.RefreshRate.ToString();
  }

  private async void FetchTimerDelegate(object? client)
  {
    // TODO: throw an exception if arg is not of HttpClient type
    var dev = (WeatherDevice)_device;
    var _client = (HttpClient)client!;
    var measurement = (WeatherDevice.WeatherMeasurement)await dev.GetMeasurement(_client);
    Dispatcher.BeginInvoke(() =>
    {
      TemperatureBlock.Text = measurement.Temperature.ToString(CultureInfo.InvariantCulture);
      HumidityBlock.Text = measurement.Humidity.ToString();
    });
  }

  private  void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    // TODO: need to handle timer disposal when you switch to a different device
    // TODO: need to change this cast and in FetchTimerDelegate()
    _ = new Timer(FetchTimerDelegate, _client, 0, _device.RefreshRate * 1000);
  }
}