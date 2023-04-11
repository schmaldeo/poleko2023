using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CsvHelper;
using Microsoft.Win32;
// ReSharper disable InconsistentNaming

namespace PolEko.ui;

public class DeviceControl : UserControl
{
  public enum Status
  {
    Ready,
    Fetching,
    Error
  }
}

public class DeviceControl<TDevice, TMeasurement, TOwner> : DeviceControl, IDeviceControl<TDevice> 
  where TDevice : Device
  where TMeasurement : Measurement
  where TOwner : DeviceControl<TDevice, TMeasurement, TOwner>
{
  public static readonly DependencyProperty DeviceProperty =
    DependencyProperty.Register(nameof(Device), typeof(TDevice), typeof(TOwner));

  public static readonly DependencyProperty HttpClientProperty =
    DependencyProperty.Register(nameof(HttpClient), typeof(HttpClient), typeof(TOwner));

  private bool _disposed;
  private Timer? _timer;
  private bool _timerDisposed;
  protected HttpClient _httpClient = null!;
  private Status _status;
  private byte _retryCounter;
  // TODO: type safety
  protected dynamic _device = null!;
  private List<TMeasurement> _measurements = new();
  
  public TDevice Device
  {
    get => (TDevice)GetValue(DeviceProperty);
    init => SetValue(DeviceProperty, value);
  }

  public HttpClient? HttpClient
  {
    get => (HttpClient)GetValue(HttpClientProperty);
    set => SetValue(HttpClientProperty, value);
  }


  public List<TMeasurement> Measurements
  {
    get => _measurements;
    protected set
    {
      _measurements = value;
      OnPropertyChanged();
    }
  }

  public Status CurrentStatus
  {
    get => _status;
    set
    {
      _status = value;
      OnPropertyChanged();
    }
  }

  protected DeviceControl()
  {
    CurrentStatus = Status.Ready;
    Loaded += delegate
    {
      _httpClient = HttpClient ?? new HttpClient();
      _device = Device;
    };
  }

  protected DeviceControl(TDevice device, HttpClient client)
  {
    Device = device;
    _device = device;
    HttpClient = client;
    _httpClient = client;
  }
  
  public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;
  
  public event PropertyChangedEventHandler? PropertyChanged;
  
  private void OnPropertyChanged([CallerMemberName] string? name = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }

  private void OnDeviceRemoved()
  {
    DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(_device));
  }

  public async ValueTask DisposeAsync()
  {
    if (_timer == null || _disposed) return;
    await _device.InsertMeasurementsAsync();
    _disposed = true;
    GC.SuppressFinalize(this);
    await _timer.DisposeAsync();
  }

  public async void Dispose()
  {
    if (_timer == null || _disposed) return;
    await _device.InsertMeasurementsAsync();
    _disposed = true;
    GC.SuppressFinalize(this);
    _timer.Dispose();
  }
  
  // By initialising the timer with period of Timeout.Infinite and then changing the timer after each request we're
  // basically making it so that the timeout is whatever is set in the callback + time that the GetMeasurementAsync task
  // took. This avoids a behaviour where if it's initialised with 1s (or whatever low value) and it's changed if the
  // request timed out, the first 2 or 3 Tasks are still executed in the 1s interval. Doing it this way just provides
  // better control over the Timer. Also see https://stackoverflow.com/a/12797382/16579633
  private async void TimerCallback(object? _)
  {
    var measurement = await _device.GetMeasurementAsync(_httpClient);
    
    if (measurement.NetworkError)
    {
      CurrentStatus = Status.Error;
      
      // Increase the timer interval to 5 seconds when there's an error
      if (!_timerDisposed) _timer?.Change(5000, Timeout.Infinite);

      if (_retryCounter < 5)
      {
        if (_retryCounter == 0) MessageBox.Show("Request timed out. Retrying 5 more times in 5 seconds intervals");
        _retryCounter++;
      }
      else
      {
        MessageBox.Show("Request timed out 5 times, aborting");
        if (_timer is not null && !_timerDisposed) await _timer.DisposeAsync();
        _retryCounter = 0;
        CurrentStatus = Status.Ready;
      }

      return;
    }

    // TODO: move this to device-specific code, make this method virtual
    if (measurement.Error)
    {
      CurrentStatus = Status.Error;
      MessageBox.Show("Device error");
      return;
    }

    // If connection was restored, put the previous timer params back
    if (CurrentStatus == Status.Error) _retryCounter = 0;

    if (!_timerDisposed) _timer?.Change(_device.RefreshRate * 1000, Timeout.Infinite);
    CurrentStatus = Status.Fetching;
  }

  protected void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    if (CurrentStatus == Status.Fetching) return;
    _timer = new Timer(TimerCallback, null, 0, Timeout.Infinite);
    _timerDisposed = false;
    CurrentStatus = Status.Fetching;
  }

  protected async void StopFetching_OnClick(object sender, RoutedEventArgs e)
  {
    if (_timer is null) return;
    await _timer.DisposeAsync();
    _timerDisposed = true;
    CurrentStatus = Status.Ready;
  }

  protected void DeleteDevice_OnClick(object sender, RoutedEventArgs e)
  {
    OnDeviceRemoved();
    Dispose();
  }

  protected async void CsvExport_Click(object sender, RoutedEventArgs e)
  {
    var dialog = new SaveFileDialog
    {
      FileName = "measurements",
      DefaultExt = "csv",
      Filter = "CSV (Comma delimited)|*.csv"
    };
    var result = dialog.ShowDialog();
    if (result != true) return;
    await using var writer = new StreamWriter(dialog.FileName);
    await using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
    await csvWriter.WriteRecordsAsync(_measurements);
  }
}

internal interface IDeviceControl<out T> : IDisposable, IAsyncDisposable, INotifyPropertyChanged where T : Device
{
  T Device { get; }
  HttpClient? HttpClient { get; set; }
  
  event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;
}


public class DeviceRemovedEventArgs : EventArgs
{
  public DeviceRemovedEventArgs(Device device)
  {
    Device = device;
  }

  public Device Device { get; }
}

public class DeviceModifiedEventArgs : DeviceRemovedEventArgs
{
  public DeviceModifiedEventArgs(Device device) : base(device) { }
}

[ValueConversion(typeof(DeviceControl.Status), typeof(bool))]
public class SmartProStatusToBoolConverter : IValueConverter 
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var status = (DeviceControl.Status)value;
    return status != DeviceControl.Status.Fetching && status != DeviceControl.Status.Error;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var val = (bool)value;
    return val ? DeviceControl.Status.Fetching : DeviceControl.Status.Ready;
  }
}