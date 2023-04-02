using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace PolEko;

// TODO: disable fetch/stop buttons based on _status, throw some exception if Device is not specified
// Will create own HttpClient if not provided, not recommended
public partial class SmartProDeviceControl : IDisposable, IAsyncDisposable
{
  public static readonly DependencyProperty DeviceProperty =
    DependencyProperty.Register(nameof(Device), typeof(SmartProDevice), typeof(SmartProDeviceControl));

  public static readonly DependencyProperty HttpClientProperty =
    DependencyProperty.Register(nameof(HttpClient), typeof(HttpClient), typeof(SmartProDeviceControl));

  private SmartProDevice? _device;
  private bool _disposed;
  private HttpClient? _httpClient;
  private byte _retryCounter;
  private Status _status;
  private Timer? _timer;

  public SmartProDeviceControl()
  {
    InitializeComponent();
    CurrentStatus = Status.Ready;
    Loaded += delegate
    {
      _httpClient = HttpClient ?? new HttpClient();
      _device = Device;
    };
  }

  public SmartProDevice Device
  {
    get => (SmartProDevice)GetValue(DeviceProperty);
    init => SetValue(DeviceProperty, value);
  }

  public HttpClient? HttpClient
  {
    get => (HttpClient)GetValue(HttpClientProperty);
    init => SetValue(HttpClientProperty, value);
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
          Dispatcher.Invoke(() => { StatusItem.Content = "Fetching"; });
          break;
        case Status.Ready:
          Dispatcher.Invoke(() => { StatusItem.Content = "Ready"; });
          break;
        case Status.Error:
          Dispatcher.Invoke(() => { StatusItem.Content = $"Request timed out {_retryCounter.ToString()} times"; });
          break;
      }
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_timer == null || _disposed) return;
    await _device!.InsertMeasurementsAsync();
    _disposed = true;
    GC.SuppressFinalize(this);
    await _timer.DisposeAsync();
  }

  // TODO: async void?!?!?!?!
  public async void Dispose()
  {
    if (_timer == null || _disposed) return;
    await _device!.InsertMeasurementsAsync();
    _disposed = true;
    GC.SuppressFinalize(this);
    _timer.Dispose();
  }

  public event EventHandler<RemoveDeviceEventArgs>? DeviceRemoved;

  private async void FetchTimerDelegate(object? _)
  {
    var measurement = await _device!.GetMeasurementAsync(_httpClient!);

    if (measurement.NetworkError)
    {
      // Increase the timer interval to 5 seconds when there's an error
      CurrentStatus = Status.Error;
      _timer!.Change(5000, 5000);

      if (_retryCounter < 5)
      {
        _retryCounter++;
      }
      else
      {
        await _timer!.DisposeAsync();
        MessageBox.Show("Request timed out 5 times, aborting");
        CurrentStatus = Status.Ready;
      }

      return;
    }

    if (measurement.Error)
    {
      await Dispatcher.BeginInvoke(() =>
      {
        TemperatureBlock.Text = "Error";
        IsRunningBlock.Text = measurement.IsRunning.ToString();
      });

      return;
    }

    // If connection was restored, put the previous timer params back
    if (CurrentStatus == Status.Error)
    {
      _timer!.Change(_device.RefreshRate * 1000, _device.RefreshRate * 1000);
      _retryCounter = 0;
    }

    CurrentStatus = Status.Fetching;

    // await Dispatcher.BeginInvoke(() =>
    // {
    //   // Parse to float with and then display as string with 2 decimal places (by default it would display x.x0 as x.x)
    //   var parsedTemperature = (float)measurement.Temperature / 100;
    //   TemperatureBlock.Text = parsedTemperature.ToString("N2");
    //   IsRunningBlock.Text = measurement.IsRunning.ToString();
    // });
  }

  private void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    if (CurrentStatus == Status.Fetching) return;
    _timer = new Timer(FetchTimerDelegate, null, 0, _device!.RefreshRate * 1000);
    CurrentStatus = Status.Fetching;
  }

  private async void StopFetching_OnClick(object sender, RoutedEventArgs e)
  {
    if (_timer is null) return;
    await _timer.DisposeAsync();
    CurrentStatus = Status.Ready;
    // TODO: remove this, test
    DataGrid.ItemsSource = _device!.MeasurementBuffer;
  }

  private void DeleteDevice_OnClick(object sender, RoutedEventArgs e)
  {
    DeviceRemoved?.Invoke(this, new RemoveDeviceEventArgs(_device!));
    Dispose();
  }

  private enum Status
  {
    Ready,
    Fetching,
    Error
  }

  public class RemoveDeviceEventArgs : EventArgs
  {
    public RemoveDeviceEventArgs(Device device)
    {
      Device = device;
    }

    public Device Device { get; }
  }
}

[ValueConversion(typeof(int), typeof(string))]
public class SmartProTemperatureConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var temperature = (int)value;
    var parsedTemperature = (float)temperature / 100;
    return parsedTemperature.ToString("N2");
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var strValue = value as string;
    if (float.TryParse(strValue, out var resultFloat)) return (int)resultFloat * 100;
    return DependencyProperty.UnsetValue;
  }
}