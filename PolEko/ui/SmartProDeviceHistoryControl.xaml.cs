using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using PolEko.util;

namespace PolEko.ui;

public partial class SmartProDeviceHistoryControl : INotifyPropertyChanged
{
  private List<SmartProMeasurement> _measurements = new();

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
    if (StartingDatePicker.Value is null || EndingDatePicker.Value is null) return;
    var measurements =
      await Database.GetMeasurementsAsync<SmartProMeasurement>((DateTime)StartingDatePicker.Value,
        (DateTime)EndingDatePicker.Value, Device!);
    Measurements = measurements;
  }
}