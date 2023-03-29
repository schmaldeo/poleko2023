using System;
using System.Net.Http;
using System.Threading;
using System.Windows;

namespace PolEko;

public partial class SmartProDeviceControl : IDisposable
{
  private readonly HttpClient _httpclient;
  private readonly SmartProDevice _device;
  private Timer? _timer;
  private byte _retryCounter;
  private bool _disposed;
  private Status _status;

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
  /// <param name="removeCallback">Delegate to be called when a device is removed</param>
  public SmartProDeviceControl(SmartProDevice device,
    in HttpClient httpclient, 
    Action<Device> removeCallback)
  {
    _device = device;
    _httpclient = httpclient;
    _removeCallback = removeCallback;
    
    InitializeComponent();
    DeviceString.Text = device.ToString();
    CurrentStatus = Status.Ready;
  }

  private Status CurrentStatus
  {
    get => _status;
    set
    {
      _status = value;
      switch (value)
      {
        case Status.Fetching:
          Dispatcher.Invoke(() =>
          {
            StatusItem.Content = "Fetching";
          });
          break;
        case Status.Ready:
          Dispatcher.Invoke(() =>
          {
            StatusItem.Content = "Ready";
          });
          break;
        case Status.Error:
          Dispatcher.Invoke(() =>
          {
            StatusItem.Content = $"Request timed out {_retryCounter.ToString()} times";
          });
          break;
      }
    }
  }

  private async void FetchTimerDelegate(object? _)
  {
    var measurement = await _device.GetMeasurement(_httpclient);
    
    if (measurement.Error)
    {
      // Increase the timer interval to 5 seconds when there's an error
      CurrentStatus = Status.Error;
      _timer!.Change(5000, 5000);
      
      // If it's the first measurement, increment
      if (_device.LastMeasurement is null)
      {
        _retryCounter = 0;
        return;
      }
      
      // If there were any errors before, but not enough to dispose the timer, reset the counter
      if (_device.LastMeasurement is not null && !_device.LastMeasurement.Error) _retryCounter = 0;
      
      if (_retryCounter < 5)
      {
        _retryCounter++;
      }
      else
      {
        await _timer!.DisposeAsync();
        _retryCounter = 0;
        CurrentStatus = Status.Ready;
      }
      
      return;
    }

    // If connection was restored, put the previous timer params back
    if (CurrentStatus == Status.Error)
    {
      _timer!.Change(_device.RefreshRate * 1000, _device.RefreshRate * 1000);
      _retryCounter = 0;
    }
    CurrentStatus = Status.Fetching;

    await Dispatcher.BeginInvoke(() =>
    {
      // Parse to float with and then display as string with 2 decimal places (by default it would display x.x0 as x.x)
      var parsedTemperature = (float)measurement.Temperature / 100;
      TemperatureBlock.Text = parsedTemperature.ToString("N2");
      IsRunningBlock.Text = measurement.IsRunning.ToString();
    });
  }

  private void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    if (CurrentStatus == Status.Fetching) return;
    _timer = new Timer(FetchTimerDelegate, null, 0, _device.RefreshRate * 1000);
    CurrentStatus = Status.Fetching;
  }
  
  private async void StopFetching_OnClick(object sender, RoutedEventArgs e)
  {
    if (_timer is null) return;
    await _timer.DisposeAsync();
    CurrentStatus = Status.Ready;
  }

  private void DeleteDevice_OnClick(object sender, RoutedEventArgs e)
  {
    _removeCallback(_device);
    Dispose();
  }

  public async void Dispose()
  {
    if (_timer == null || _disposed) return;
    _device.InsertMeasurements();
    _disposed = true;
    GC.SuppressFinalize(this);
    await _timer.DisposeAsync();
  }
}