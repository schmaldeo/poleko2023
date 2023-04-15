using System;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Data;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PolEko.util;

namespace PolEko.ui;

public partial class SmartProDeviceControl
{
  #region Fields
  
  private HistoricalDataStatus _dataStatus = HistoricalDataStatus.Waiting;

  private PlotModel? _plotModel;
  
  #endregion

  #region Constructors
  
  public SmartProDeviceControl()
  {
    InitializeComponent();
  }

  public SmartProDeviceControl(SmartProDevice device, HttpClient httpClient) : base(device, httpClient)
  {
    InitializeComponent();
  }
  
  #endregion
  
  #region Properties

  public HistoricalDataStatus DataStatus
  {
    get => _dataStatus;
    set
    {
      _dataStatus = value;
      OnPropertyChanged();
    }
  }

  public PlotModel PlotModel
  {
    get => _plotModel ?? new PlotModel();
    set
    {
      // This is required by OxyPlot design: https://oxyplot.readthedocs.io/en/latest/common-tasks/refresh-plot.html
      _plotModel?.InvalidatePlot(true);
      _plotModel = value;
    }
  }
  
  #endregion
  
  #region Methods

  private async void Button_Click(object sender, RoutedEventArgs e)
  {
    if (StartingDatePicker.Value is null || EndingDatePicker.Value is null)
    {
      var str = (string)Application.Current.FindResource("SpecifyStartEndDate")!;
      MessageBox.Show(str);
      return;
    }

    DataStatus = HistoricalDataStatus.Fetching;
    var measurements =
      await Database.GetMeasurementsAsync<SmartProMeasurement>((DateTime)StartingDatePicker.Value,
        (DateTime)EndingDatePicker.Value, _device);
    DataStatus = HistoricalDataStatus.Showing;
    Measurements = measurements;

    var plotModel = new PlotModel();


    plotModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Time" });
    plotModel.Axes.Add(new LinearAxis
    {
      Position = AxisPosition.Left,
      Title = "Temperature",
      MinimumRange = 30,
      MaximumRange = 40
    });

    // Use LineSeries with a Decimator because the performance is tragic without it when amount of data is big. Tradeoff
    // is that there will be a line between 2 points if there's missing data. That's something that could be solved by
    // filling the gaps with Double.NaN.
    var lineSeries = new LineSeries
    {
      Decimator = Decimator.Decimate
    };


    foreach (var measurement in measurements)
      lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(measurement.TimeStamp), Math.Round(
        (float)measurement.Temperature / 100, 2)));

    plotModel.Series.Add(lineSeries);
    PlotModel = plotModel;
  }
  
  #endregion
}

/// <summary>
/// Divides an integer by 100 and converts it to a string with 2 decimal places 
/// </summary>
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

/// <summary>
/// Converts status of <see cref="DeviceControl{TDevice,TMeasurement,TOwner}"/> to a string
/// </summary>
[ValueConversion(typeof(DeviceControl.Status), typeof(string))]
public class DeviceStatusToStringConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var status = (DeviceControl.Status)value;
    return status switch
    {
      DeviceControl.Status.Error => (string)Application.Current.FindResource("Error")!,
      DeviceControl.Status.NetworkError => (string)Application.Current.FindResource("NetworkError")!,
      DeviceControl.Status.Fetching => (string)Application.Current.FindResource("Fetching")!,
      DeviceControl.Status.Ready => (string)Application.Current.FindResource("Ready")!,
      _ => "Unknown"
    };
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var val = (string)value;
    return val switch
    {
      "Error" => DeviceControl.Status.Error,
      "Fetching" => DeviceControl.Status.Fetching,
      "Ready" => DeviceControl.Status.Ready,
      _ => DependencyProperty.UnsetValue
    };
  }
}

/// <summary>
/// Converts <see cref="HistoricalDataStatus"/> to a boolean
/// </summary>
[ValueConversion(typeof(HistoricalDataStatus), typeof(bool))]
public class FetchingStatusToBoolConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var status = (HistoricalDataStatus)value;
    return status is not HistoricalDataStatus.Fetching;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var val = (bool)value;
    return val ? HistoricalDataStatus.Waiting : HistoricalDataStatus.Fetching;
  }
}