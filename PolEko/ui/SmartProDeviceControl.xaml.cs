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
using System.Windows.Data;
using CsvHelper;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PolEko.util;

namespace PolEko.ui;

// Will create own HttpClient if not provided, not recommended
public partial class SmartProDeviceControl : IDeviceControl<SmartProDevice>
{
  public static readonly DependencyProperty DeviceProperty =
    DependencyProperty.Register(nameof(Device), typeof(SmartProDevice), typeof(SmartProDeviceControl));

  public static readonly DependencyProperty HttpClientProperty =
    DependencyProperty.Register(nameof(HttpClient), typeof(HttpClient), typeof(SmartProDeviceControl));

  private List<SmartProMeasurement> _measurements = new();
  private bool _disposed;
  private HttpClient? _httpClient;
  private byte _retryCounter;
  private Status _status;
  private Timer? _timer;
  
  public List<SmartProMeasurement> Measurements
  {
    get => _measurements;
    private set
    {
      _measurements = value;
      OnPropertyChanged();
    }
  }

  private PlotModel PlotModel
  {
    set =>
      // This is required by OxyPlot design: https://oxyplot.readthedocs.io/en/latest/common-tasks/refresh-plot.html
      PlotView.Model = value;
  }
  
  public event PropertyChangedEventHandler? PropertyChanged;
  
  public event EventHandler<RemoveDeviceEventArgs>? DeviceRemoved;

  public SmartProDeviceControl()
  {
    InitializeComponent();
    CurrentStatus = Status.Ready;
    Loaded += delegate
    {
      _httpClient = HttpClient ?? new HttpClient();
    };
  }

  public SmartProDeviceControl(SmartProDevice device, HttpClient httpClient)
  {
    Device = device;
    HttpClient = httpClient;
    InitializeComponent();
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

  public Status CurrentStatus
  {
    get => _status;
    set
    {
      _status = value;
      OnPropertyChanged();
    }
  }

  private void OnPropertyChanged([CallerMemberName] string? name = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
  
  public async ValueTask DisposeAsync()
  {
    if (_timer == null || _disposed) return;
    await Device.InsertMeasurementsAsync();
    _disposed = true;
    GC.SuppressFinalize(this);
    await _timer.DisposeAsync();
  }

  public async void Dispose()
  {
    if (_timer == null || _disposed) return;
    await Device.InsertMeasurementsAsync();
    _disposed = true;
    GC.SuppressFinalize(this);
    _timer.Dispose();
  }

  private async void TimerCallback(object? _)
  {
    var measurement = await Device.GetMeasurementAsync(_httpClient!);

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
      // TODO: mvvm
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
      _timer?.Change(Device.RefreshRate * 1000, Device.RefreshRate * 1000);
      _retryCounter = 0;
    }

    CurrentStatus = Status.Fetching;
  }

  private void FetchData_OnClick(object sender, RoutedEventArgs e)
  {
    if (CurrentStatus == Status.Fetching) return;
    _timer = new Timer(TimerCallback, null, 0, Device.RefreshRate * 1000);
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
    DeviceRemoved?.Invoke(this, new RemoveDeviceEventArgs(Device));
    Dispose();
  }
  
  private async void Button_Click(object sender, RoutedEventArgs e)
  {
    // TODO
    if (StartingDatePicker.Value is null || EndingDatePicker.Value is null) return;
    var measurements =
      await Database.GetMeasurementsAsync<SmartProMeasurement>((DateTime)StartingDatePicker.Value,
        (DateTime)EndingDatePicker.Value, Device);
    Measurements = measurements;
    var plotModel = new PlotModel();

    // Add X and Y axes to the plot
    plotModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Time" });
    plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Temperature" });

    // Create a new LineSeries object
    var lineSeries = new LineSeries();

    // Add data points to the LineSeries from the List<DeviceMeasurement> object
    foreach (var measurement in measurements)
    {
      lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(measurement.TimeStamp), Math.Round(
        (float)measurement.Temperature / 100, 2)));
    }

    // Add the LineSeries to the PlotModel
    plotModel.Series.Add(lineSeries);
    PlotModel = plotModel;
  }
  
  private async void CsvExport_Click(object sender, RoutedEventArgs e)
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

  public enum Status
  {
    Ready,
    Fetching,
    Error
  }
}

public class RemoveDeviceEventArgs : EventArgs
{
  public RemoveDeviceEventArgs(Device device)
  {
    Device = device;
  }

  public Device Device { get; }
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

[ValueConversion(typeof(SmartProDeviceControl.Status), typeof(bool))]
public class SmartProStatusToBoolConverter : IValueConverter 
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var status = (SmartProDeviceControl.Status)value;
    return status is not SmartProDeviceControl.Status.Fetching or SmartProDeviceControl.Status.Error;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var val = (bool)value;
    return val ? SmartProDeviceControl.Status.Fetching : SmartProDeviceControl.Status.Ready;
  }
}

[ValueConversion(typeof(SmartProDeviceControl.Status), typeof(string))]
public class SmartProStatusToStringConverter : IValueConverter 
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var status = (SmartProDeviceControl.Status)value;
    return status switch
    {
      SmartProDeviceControl.Status.Error => "Error",
      SmartProDeviceControl.Status.Fetching => "Fetching",
      SmartProDeviceControl.Status.Ready => "Ready",
      _ => "Unknown"
    };
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var val = (string)value;
    return val switch
    {
      "Error" => SmartProDeviceControl.Status.Error,
      "Fetching" => SmartProDeviceControl.Status.Fetching,
      "Ready" => SmartProDeviceControl.Status.Ready,
      _ => DependencyProperty.UnsetValue
    };
  }
}
