﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace PolEko;

/// <summary>
///   Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
  private Device? _currentDevice;
  private SmartProDeviceControl? _deviceInfo;
  private HttpClient? _httpClient;

  public static readonly DependencyProperty TypesProperty =
    DependencyProperty.Register(nameof(Types), typeof(Dictionary<string, Type>), typeof(IpPrompt));
  
  public Dictionary<string, Type>? Types
  {
    get => (Dictionary<string, Type>)GetValue(TypesProperty);
    init => SetValue(TypesProperty, value);
  }
  
  public static readonly DependencyProperty DevicesProperty =
    DependencyProperty.Register(nameof(Devices), typeof(ObservableCollection<Device>), typeof(MainWindow));

  public ObservableCollection<Device>? Devices
  {
    get => (ObservableCollection<Device>)GetValue(DevicesProperty);
    set => SetValue(DevicesProperty, value);
  }
  
  public static readonly DependencyProperty OpenDevicesProperty =
    DependencyProperty.Register(nameof(OpenDevices), typeof(ObservableCollection<TabItem>), typeof(MainWindow));

  public ObservableCollection<TabItem>? OpenDevices
  {
    get => (ObservableCollection<TabItem>)GetValue(OpenDevicesProperty);
    set => SetValue(OpenDevicesProperty, value);
  }
  
  public static readonly DependencyProperty SelectedDeviceControlProperty =
    DependencyProperty.Register(nameof(SelectedDeviceControl), typeof(TabItem), typeof(MainWindow));

  public TabItem? SelectedDeviceControl
  {
    get => (TabItem)GetValue(SelectedDeviceControlProperty);
    set => SetValue(SelectedDeviceControlProperty, value);
  }
  
  private Dictionary<Device, TabItem> DeviceControls { get; } = new();

  public MainWindow()
  {
    InitializeComponent();
    Loaded += delegate { Devices ??= new ObservableCollection<Device>(); };
  }

  private void HandleDeviceChange(object sender, RoutedEventArgs e)
  {
    // Check for potential invalid args
    if (sender is not ListBoxItem value) throw new ArgumentException("You can only use this method to handle ListBoxItem Click event");
    if (value.Content is not Device incomingDevice) throw new ArgumentException("Button's content can only be of type Device");

    // Disallow reopening a device that's currently open
    if (_currentDevice is not null && _currentDevice.Equals(incomingDevice)) return;

    _currentDevice = incomingDevice;
    var httpClient = _httpClient ??= new HttpClient();
    // TODO: Reflection
    if (_currentDevice is SmartProDevice device)
    {
      _deviceInfo = new(device, httpClient, RemoveDevice);
      Closing += async delegate { await _deviceInfo.DisposeAsync(); };
    }

    if (_currentDevice is ExampleDevice)
    {
      MessageBox.Show("Unimplemented");
      return;
    }

    var formattedHeader = $"{_currentDevice.IpAddress}:{_currentDevice.Port}"; 
    var item = new TabItem
    {
      Content = _deviceInfo,
      Header = formattedHeader
    };
    
    
    if (DeviceControls.ContainsKey(incomingDevice))
    {
      SelectedDeviceControl = DeviceControls[incomingDevice];
      return;
    }
    DeviceControls[incomingDevice] = item;
    
    OpenDevices ??= new ObservableCollection<TabItem>();
    OpenDevices.Add(item);
    SelectedDeviceControl = item;
  }
  
  private async void AddNewDevice(IPAddress ipAddress, ushort port, string? id, Type type)
  {
    var instance = Activator.CreateInstance(type, ipAddress, port, id);
    if (instance is null) throw new Exception("Null instance returned from Activator.CreateInstance() call");
    var device = (Device)instance;
    if (Devices!.Contains(device))
    {
      MessageBox.Show("Urządzenie już istnieje");
      return;
    }

    await Database.AddDeviceAsync(device, type);
    Devices.Add(device);
  }
  
  private async void RemoveDevice(Device device)
  {
    await Database.RemoveDeviceAsync(device);
    Devices!.Remove(device);
  }
  
  private void AddNewDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new()
    {
      Types = Types,
      Callback = AddNewDevice
    };
    prompt.Show();
  }
}