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

  public PlotModel? PlotModel { get; set; }

  #endregion
  
  #region Event handlers

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
    

    if (PlotModel is null)
    {
      PlotModel = new PlotModel();
      PlotModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Time" });
      PlotModel.Axes.Add(new LinearAxis
      {
        Position = AxisPosition.Left,
        Title = "Temperature",
        MinimumRange = 30,
        MaximumRange = 40
      });
      
      PlotModel.Series.Add(lineSeries);
    }
    else
    {
      PlotModel.Series.RemoveAt(0);
      PlotModel.Series.Add(lineSeries);
      
      PlotModel.InvalidatePlot(true);
    }
  }
  
  #endregion
}

/// <summary>
/// \~english Divides an integer by 100 and converts it to a string with 2 decimal places
/// \~polish Dzieli liczbę przez 100 i konwertuje ją na ciąg znaków z dwoma miejscami po przecinku
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
/// \~english Converts status of <see cref="DeviceControl"/> to a string
/// \~polish Konwertuje status <see cref="DeviceControl"/> na ciąg znaków
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
    throw new NotImplementedException();
  }
}

/// <summary>
/// \~english Converts <see cref="HistoricalDataStatus"/> to a boolean
/// \~polish Konwertuje status <see cref="HistoricalDataStatus"/> do typu logicznego
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