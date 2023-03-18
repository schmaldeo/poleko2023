using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Windows;

namespace PolEko;

public partial class WeatherDeviceInfoControl : IDisposable
{
  private readonly HttpClient _httpclient;
  // TODO: modify _device when EditDevice is called
  private readonly Device _device;
  private Timer? _timer;
  private byte _retryCounter;
  private bool _disposed;
  private Status _status;

  private readonly Action<IPAddress, ushort, string?> _editCallback;
  private readonly Action<Device> _removeCallback;

  private enum Status
  {
    Ready,
    Fetching,
    Error
  }
  
  /// <summary>
  /// Control displaying <c>Device</c>'s parameters and allowing to edit its parameters, as well as fetch measurements
  /// </summary>
  /// <param name="device"><c>Device</c> whose parameters will be displayed</param>
  /// <param name="httpclient"><c>HttpClient</c> that will be used to fetch measurements, passed in by reference</param>
  /// <param name="editCallback">Delegate to be called when a device is edited</param>
  /// <param name="removeCallback">Delegate to be called when a device is removed</param>
  public WeatherDeviceInfoControl(Device device,
    in HttpClient httpclient, 
    Action<IPAddress, ushort, string?> editCallback, 
    Action<Device> removeCallback)
  {
    _device = device;
    _httpclient = httpclient;
    _editCallback = editCallback;
    _removeCallback = removeCallback;
    
    InitializeComponent();
    DeviceString.Text = device.ToString();
  }

  private async void FetchTimerDelegate(object? _)
  {
    // TODO: clean up this cast mess
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
    _timer = new Timer(FetchTimerDelegate, null, 0, _device.RefreshRate * 1000);
    _status = Status.Fetching;
  }
  
  private async void StopFetching_OnClick(object sender, RoutedEventArgs e)
  {
    if (_timer is null) return;
    await _timer.DisposeAsync();
    _status = Status.Ready;
    // TODO: upload buffer
  }

  // TODO: need to update UI after device is edited and edit the db entry
  private void EditDevice_OnClick(object sender, RoutedEventArgs e)
  {
    var prompt = new IpPrompt(_editCallback, _device.IpAddress, _device.Port, _device.Id);
    prompt.Show();
  }
  
  private void DeleteDevice_OnClick(object sender, RoutedEventArgs e)
  {
    _removeCallback(_device);
  }

  public async void Dispose()
  {
    if (_timer == null || _disposed) return;
    _disposed = true;
    GC.SuppressFinalize(this);
    await _timer.DisposeAsync();
  }
}