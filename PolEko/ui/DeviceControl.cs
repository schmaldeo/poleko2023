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

public class DeviceControl<TDevice, TMeasurement> : DeviceControl, IDeviceControl<TDevice> 
  where TDevice : Device
  where TMeasurement : Measurement
{
  public static readonly DependencyProperty DeviceProperty =
    DependencyProperty.Register(nameof(Device), typeof(TDevice), typeof(DeviceControl));

  public static readonly DependencyProperty HttpClientProperty =
    DependencyProperty.Register(nameof(HttpClient), typeof(HttpClient), typeof(DeviceControl));

  
  private bool _disposed;
  private Timer? _timer;
  protected HttpClient _httpClient;
  private Status _status;
  private byte _retryCounter;
  // TODO: type safety
  protected dynamic _device;
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

  public DeviceControl()
  {
    CurrentStatus = Status.Ready;
    Loaded += delegate
    {
      _httpClient = HttpClient ?? new HttpClient();
      _device = Device;
    };
  }

  public DeviceControl(TDevice device, HttpClient client)
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
  
  private async void TimerCallback(object? _)
  {
    var measurement = await _device.GetMeasurementAsync(_httpClient);

    if (measurement.NetworkError)
    {
      // Increase the timer interval to 5 seconds when there's an error
      CurrentStatus = Status.Error;
      if (_timer is null) return;
      _timer.Change(5000, 5000);

      if (_retryCounter < 5)
      {
        _retryCounter++;
      }
      else
      {
        if (_timer is not null) await _timer.DisposeAsync();
        MessageBox.Show("Request timed out 5 times, aborting");
        CurrentStatus = Status.Ready;
      }

      return;
    }

    if (measurement.Error)
    {
      CurrentStatus = Status.Error;
      return;
    }

    // If connection was restored, put the previous timer params back
    if (CurrentStatus == Status.Error)
    {
      _timer?.Change(_device.RefreshRate * 1000, _device.RefreshRate * 1000);
      _retryCounter = 0;
    }

    CurrentStatus = Status.Fetching;
  }

  protected void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    if (CurrentStatus == Status.Fetching) return;
    _timer = new Timer((TimerCallback)TimerCallback, null, 0, _device.RefreshRate * 1000);
    CurrentStatus = Status.Fetching;
  }

  protected async void StopFetching_OnClick(object sender, RoutedEventArgs e)
  {
    if (_timer is null) return;
    await _timer.DisposeAsync();
    CurrentStatus = Status.Ready;
  }

  protected void DeleteDevice_OnClick(object sender, RoutedEventArgs e)
  {
    DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(_device));
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
  HttpClient HttpClient { get; set; }
  
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

[ValueConversion(typeof(DeviceControl.Status), typeof(bool))]
public class SmartProStatusToBoolConverter : IValueConverter 
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var status = (DeviceControl.Status)value;
    return status is not DeviceControl.Status.Fetching or DeviceControl.Status.Error;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var val = (bool)value;
    return val ? DeviceControl.Status.Fetching : DeviceControl.Status.Ready;
  }
}