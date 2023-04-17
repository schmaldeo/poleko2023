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

public abstract class DeviceControl : UserControl
{
  public enum Status
  {
    Ready,
    Fetching,
    Error,
    NetworkError
  }
}

public class DeviceControl<TDevice, TMeasurement, TOwner> : DeviceControl, IDeviceControl<TDevice>
  where TDevice : Device
  where TMeasurement : Measurement
  where TOwner : DeviceControl<TDevice, TMeasurement, TOwner>
{
  #region DependencyProperties
  
  public static readonly DependencyProperty DeviceProperty =
    DependencyProperty.Register(nameof(Device), typeof(TDevice), typeof(TOwner));

  public static readonly DependencyProperty HttpClientProperty =
    DependencyProperty.Register(nameof(HttpClient), typeof(HttpClient), typeof(TOwner));
  
  #endregion

  #region Fields
  
  // Even though it is of dynamic type, it cannot be assigned to something that isn't of type Device, as it's 
  // assigned from the Device DependencyProperty that's of type Device itself
  protected dynamic _device = null!;

  protected HttpClient _httpClient = null!;
  private IEnumerable<TMeasurement> _measurements = new List<TMeasurement>();
  private Status _status;
  private Timer? _timer;
  private bool _disposed;
  private bool _timerDisposed;
  
  /// <summary>
  /// Indicates that an error StatusBox was shown already
  /// </summary>
  private bool _errorBoxShown;
  
  #endregion

  #region Constructors
  
  protected DeviceControl()
  {
    CurrentStatus = Status.Ready;
    Loaded += delegate
    {
      _httpClient = HttpClient ?? new HttpClient();
      _device = Device ?? throw new NullReferenceException($"Device cannot be null in DeviceControl`3");
    };
  }

  protected DeviceControl(TDevice device, HttpClient client)
  {
    Device = device;
    _device = device;
    HttpClient = client;
    _httpClient = client;
  }

  #endregion

  #region Properties
  
  public IEnumerable<TMeasurement> Measurements
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

  public TDevice? Device
  {
    get => (TDevice)GetValue(DeviceProperty);
    init => SetValue(DeviceProperty, value);
  }

  public HttpClient? HttpClient
  {
    get => (HttpClient)GetValue(HttpClientProperty);
    set => SetValue(HttpClientProperty, value);
  }
  
  #endregion
  
  #region Events

  public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;
  
  protected void OnDeviceRemoved()
  {
    DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(_device));
  }

  public event PropertyChangedEventHandler? PropertyChanged;
  
  protected void OnPropertyChanged([CallerMemberName] string? name = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
  
  #endregion

  #region Methods, Event handlers
  
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
    
    if (measurement.Error || measurement.NetworkError)
    {
      if (measurement.Error && CurrentStatus != Status.Error)
      {
        CurrentStatus = Status.Error;
        _errorBoxShown = false;
      }

      if (measurement.NetworkError && CurrentStatus != Status.NetworkError)
      {
        CurrentStatus = Status.NetworkError;
        _errorBoxShown = false;
      }
      
      if (!_timerDisposed) _timer?.Change(5000, Timeout.Infinite);
      
      if (_errorBoxShown) return;
      
      string str;
      if (measurement.Error)
      {
        str = (string)Application.Current.FindResource("DeviceError")!;
      }
      else
      {
        str = (string)Application.Current.FindResource("DeviceTimedOut")!;
      }
      MessageBox.Show(str);
      _errorBoxShown = true;
      
      return;
    }

    // If connection was restored, put the previous timer params back
    if (CurrentStatus is Status.NetworkError or Status.Error) _errorBoxShown = false;

    if (!_timerDisposed) _timer?.Change(_device.RefreshRate * 1000, Timeout.Infinite);
    CurrentStatus = Status.Fetching;
  }

  protected void FetchData_Click(object sender, RoutedEventArgs e)
  {
    if (CurrentStatus == Status.Fetching) return;
    _timer = new Timer(TimerCallback, null, 0, Timeout.Infinite);
    _timerDisposed = false;
    CurrentStatus = Status.Fetching;
  }

  protected async void StopFetching_Click(object sender, RoutedEventArgs e)
  {
    if (_timer is null) return;
    await _timer.DisposeAsync();
    _timerDisposed = true;
    CurrentStatus = Status.Ready;
  }

  protected void DeleteDevice_Click(object sender, RoutedEventArgs e)
  {
    var DeleteBoxText = (string)Application.Current.FindResource("DeleteBoxText")!;
    var messageBoxResult = MessageBox.Show(DeleteBoxText, "", MessageBoxButton.YesNo);
    if (messageBoxResult != MessageBoxResult.Yes) return;
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
  
  #endregion
}

public enum HistoricalDataStatus
{
  Waiting,
  Fetching,
  Showing
}

// Purpose behind this interface was mainly to be able to cast DeviceControl to something in MainWindow.
public interface IDeviceControl<out T> : IDisposable, IAsyncDisposable, INotifyPropertyChanged where T : Device
{
  T? Device { get; }
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

/// <summary>
/// Converts status to boolean that can be used for IsEnabled property of a button.
/// Returns true if status is Fetching, Error or Network error
/// </summary>
[ValueConversion(typeof(DeviceControl.Status), typeof(bool))]
public class SmartProStatusToBoolConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var status = (DeviceControl.Status)value;
    return status != DeviceControl.Status.Fetching && status != DeviceControl.Status.Error && status != DeviceControl.Status.NetworkError;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var val = (bool)value;
    return val ? DeviceControl.Status.Fetching : DeviceControl.Status.Ready;
  }
}