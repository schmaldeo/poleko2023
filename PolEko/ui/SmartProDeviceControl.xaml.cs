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

// Will create own HttpClient if not provided, not recommended
public partial class SmartProDeviceControl
{
  private PlotModel PlotModel
  {
    set =>
      // This is required by OxyPlot design: https://oxyplot.readthedocs.io/en/latest/common-tasks/refresh-plot.html
      PlotView.Model = value;
  }

  public SmartProDeviceControl()
  {
    InitializeComponent();
  }

  public SmartProDeviceControl(SmartProDevice device, HttpClient httpClient) : base(device, httpClient)
  {
    InitializeComponent();
  }

  private async void Button_Click(object sender, RoutedEventArgs e)
  {
    if (StartingDatePicker.Value is null || EndingDatePicker.Value is null)
    {
      MessageBox.Show("Specify start and end date");
      return;
    }
    var measurements =
      await Database.GetMeasurementsAsync<SmartProMeasurement>((DateTime)StartingDatePicker.Value,
        (DateTime)EndingDatePicker.Value, _device);
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
      Decimator=Decimator.Decimate
    };
    

    foreach (var measurement in measurements)
    {
      lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(measurement.TimeStamp), Math.Round(
        (float)measurement.Temperature / 100, 2)));
    }

    plotModel.Series.Add(lineSeries);
    PlotModel = plotModel;
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

[ValueConversion(typeof(DeviceControl.Status), typeof(string))]
public class DeviceStatusToStringConverter : IValueConverter 
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var status = (DeviceControl.Status)value;
    return status switch
    {
      DeviceControl.Status.Error => "Error",
      DeviceControl.Status.Fetching => "Fetching",
      DeviceControl.Status.Ready => "Ready",
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
