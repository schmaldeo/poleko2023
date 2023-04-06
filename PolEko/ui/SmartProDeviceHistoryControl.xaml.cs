using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using CsvHelper;
using Microsoft.Win32;
using PolEko.util;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace PolEko.ui;

public partial class SmartProDeviceHistoryControl : INotifyPropertyChanged
{
  private List<SmartProMeasurement> _measurements = new();
  private PlotModel? _plotModel;

  public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
    nameof(Device), typeof(SmartProDevice), typeof(SmartProDeviceHistoryControl));

  public SmartProDevice Device
  {
    get => (SmartProDevice)GetValue(DeviceProperty);
    set => SetValue(DeviceProperty, value);
  }

  public List<SmartProMeasurement> Measurements
  {
    get => _measurements;
    private set
    {
      _measurements = value;
      OnPropertyChanged();
    }
  }

  public PlotModel PlotModel
  {
    get => _plotModel;
    private set
    {
      _plotModel = value;
      // This is required by OxyPlot design: https://oxyplot.readthedocs.io/en/latest/common-tasks/refresh-plot.html
      PlotView.Model = value;
    }
  }
  
  public event PropertyChangedEventHandler? PropertyChanged;

  public SmartProDeviceHistoryControl()
  {
    InitializeComponent();
    Loaded += delegate
    {
      if (Device is null) throw new ArgumentException("Device property must be specified");
    };
  }

  private void OnPropertyChanged([CallerMemberName] string? name = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }

  private async void Button_Click(object sender, RoutedEventArgs e)
  {
    // TODO
    if (StartingDatePicker.Value is null || EndingDatePicker.Value is null) return;
    var measurements =
      await Database.GetMeasurementsAsync<SmartProMeasurement>((DateTime)StartingDatePicker.Value,
        (DateTime)EndingDatePicker.Value, Device!);
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
}