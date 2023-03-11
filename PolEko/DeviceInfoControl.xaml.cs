using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Windows;

namespace PolEko;

public partial class DeviceInfoControl : IDisposable
{
  private readonly HttpClient _httpclient;
  private readonly Device _device;
  private Timer? _timer;
  private byte _retryCounter;
  private bool _disposed;
  private Status _status;

  private enum Status
  {
    Ready,
    Fetching,
    Error,
  }

  public DeviceInfoControl(Device device, HttpClient httpclient)
  {
    _device = device;
    _httpclient = httpclient;
    
    InitializeComponent();
    
    // TODO: replace this with actual params
    NameBlock.Text = device.ToString();
    IpBlock.Text = device.IpAddress.ToString();
    TypeBlock.Text = device.Type;
    DescriptionBlock.Text = device.Description;
    RefreshRateBlock.Text = _device.RefreshRate.ToString();
  }

  // TODO: maybe this could be called different
  private async void FetchTimerDelegate(object? _)
  {
    // TODO: clean up this cast mess
    // use pattern matching perhaps
    var dev = (WeatherDevice)_device;
    var measurement = await dev.GetMeasurement(_httpclient);
    
    if (measurement.Error)
    {
      // Increase the timer interval to 5 seconds when there's an error
      _status = Status.Error;
      _timer!.Change(5000, 5000);
      
      // If it's the first measurement, increment
      if (dev.LastMeasurement is null)
      {
        _retryCounter = 0;
        return;
      }
      
      // If there were any errors before, but not enough to dispose the timer, reset the counter
      if (dev.LastMeasurement is not null && !dev.LastMeasurement.Error) _retryCounter = 0;
      
      if (_retryCounter < 5)
      {
        _retryCounter++;
      }
      else
      {
        await _timer!.DisposeAsync();
        _retryCounter = 0;
        _status = Status.Ready;
      }

      // TODO: show this on the status bar instead
      MessageBox.Show($"Request timed out {_retryCounter} time(s)");
      return;
    }

    // If connection was restored, put the previous timer params back
    if (_status == Status.Error) _timer!.Change(_device.RefreshRate * 1000, _device.RefreshRate * 1000);
    _status = Status.Fetching;

    await Dispatcher.BeginInvoke(() =>
    {
      TemperatureBlock.Text = measurement.Temperature.ToString(CultureInfo.InvariantCulture);
      HumidityBlock.Text = measurement.Humidity.ToString();
    });
  }

  private void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    _timer = new Timer(FetchTimerDelegate, "", 0, _device.RefreshRate * 1000);
  }

  public async void Dispose()
  {
    if (_timer == null || _disposed) return;
    _disposed = true;
    GC.SuppressFinalize(this);
    await _timer.DisposeAsync();
  }
}