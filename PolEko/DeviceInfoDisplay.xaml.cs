using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Windows;

namespace PolEko;

public partial class DeviceInfoDisplay : IDisposable
{
  private readonly HttpClient _httpclient;
  private readonly Device _device;
  private Timer? _timer;
  
  public DeviceInfoDisplay(Device device, HttpClient httpclient)
  {
    _device = device;
    _httpclient = httpclient;
    
    InitializeComponent();
    
    NameBlock.Text = device.ToString();
    IpBlock.Text = device.IpAddress.ToString();
    TypeBlock.Text = device.Type;
    DescriptionBlock.Text = device.Description;
    RefreshRateBlock.Text = device.RefreshRate.ToString();
  }

  private async void FetchTimerDelegate(object? client)
  {
    // TODO: throw an exception if arg is not of HttpClient type and clean up this cast mess
    var dev = (WeatherDevice)_device;
    var _client = (HttpClient)client!;
    var measurement = (WeatherDevice.WeatherMeasurement)await dev.GetMeasurement(_client);
    await Dispatcher.BeginInvoke(() =>
    {
      TemperatureBlock.Text = measurement.Temperature.ToString(CultureInfo.InvariantCulture);
      HumidityBlock.Text = measurement.Humidity.ToString();
    });
  }

  private void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    _timer = new Timer(FetchTimerDelegate, _httpclient, 0, _device.RefreshRate * 1000);
  }

  public async void Dispose()
  {
    if (_timer != null) await _timer.DisposeAsync();
  }
}