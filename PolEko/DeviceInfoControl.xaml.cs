using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace PolEko;

public partial class DeviceInfoControl : IDisposable
{
  private readonly HttpClient _httpclient;
  // TODO: modify _device when EditDevice is called
  private readonly Device _device;
  private Timer? _timer;
  private byte _retryCounter;
  private bool _disposed;
  private Status _status;

  private readonly Action<IPAddress, ushort, string?> _editCallback;

  private enum Status
  {
    Ready,
    Fetching,
    Error,
  }
  
  /// <summary>
  /// Control displaying <c>Device</c>'s parameters and allowing to edit its parameters, as well as fetch measurements
  /// </summary>
  /// <param name="device"><c>Device</c> whose parameters will be displayed</param>
  /// <param name="httpclient"><c>HttpClient</c> that will be used to fetch measurements, passed in by reference</param>
  /// <param name="editCallback">Delegate to be called when a device is edited</param>
  public DeviceInfoControl(Device device, in HttpClient httpclient, Action<IPAddress, ushort, string?> editCallback)
  {
    _device = device;
    _httpclient = httpclient;
    _editCallback = editCallback;
    
    InitializeComponent();

    // TODO: replace this with actual params
    DeviceString.Text = device.ToString();
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
    if (_status == Status.Fetching) return;
    _timer = new Timer(FetchTimerDelegate, "", 0, _device.RefreshRate * 1000);
    _status = Status.Fetching;
  }

  // TODO: need to update UI after device is edited
  private void EditDevice_OnClick(object sender, RoutedEventArgs e)
  {
    var prompt = new IpPrompt(_editCallback, _device.IpAddress, _device.Port, _device.Id);
    prompt.Show();
  }
  
  private async void DeleteDevice_OnClick(object sender, RoutedEventArgs e)
  {
    // TODO: remove it from the Devices collection as well
    await using var connection = new SqliteConnection("Data Source=Measurements.db");
    await Database.RemoveDeviceAsync(connection, _device);
  }

  public async void Dispose()
  {
    if (_timer == null || _disposed) return;
    _disposed = true;
    GC.SuppressFinalize(this);
    await _timer.DisposeAsync();
  }
}