using System;
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
  public static readonly DependencyProperty TypesProperty =
    DependencyProperty.Register(nameof(Types), typeof(Dictionary<string, Type>), typeof(IpPrompt));

  public static readonly DependencyProperty DevicesProperty =
    DependencyProperty.Register(nameof(Devices), typeof(ObservableCollection<Device>), typeof(MainWindow));

  public static readonly DependencyProperty OpenDevicesProperty =
    DependencyProperty.Register(nameof(OpenDevices), typeof(ObservableCollection<TabItem>), typeof(MainWindow));

  public static readonly DependencyProperty SelectedDeviceControlProperty =
    DependencyProperty.Register(nameof(SelectedDeviceControl), typeof(TabItem), typeof(MainWindow));

  private Device? _currentDevice;
  private SmartProDeviceControl? _deviceInfo;
  private HttpClient? _httpClient;

  public MainWindow()
  {
    InitializeComponent();
    Loaded += delegate { Devices ??= new ObservableCollection<Device>(); };
  }

  public Dictionary<string, Type>? Types
  {
    get => (Dictionary<string, Type>)GetValue(TypesProperty);
    init => SetValue(TypesProperty, value);
  }

  public ObservableCollection<Device>? Devices
  {
    get => (ObservableCollection<Device>)GetValue(DevicesProperty);
    set => SetValue(DevicesProperty, value);
  }

  public ObservableCollection<TabItem>? OpenDevices
  {
    get => (ObservableCollection<TabItem>)GetValue(OpenDevicesProperty);
    set => SetValue(OpenDevicesProperty, value);
  }

  public TabItem? SelectedDeviceControl
  {
    get => (TabItem)GetValue(SelectedDeviceControlProperty);
    set => SetValue(SelectedDeviceControlProperty, value);
  }

  private Dictionary<Device, TabItem> DeviceControls { get; } = new();

  private void HandleDeviceChange(object sender, RoutedEventArgs e)
  {
    // Check for potential invalid args
    if (sender is not ListBoxItem value)
      throw new ArgumentException("You can only use this method to handle ListBoxItem Click event");
    if (value.Content is not Device incomingDevice)
      throw new ArgumentException("Button's content can only be of type Device");

    // Disallow reopening a device that's currently open
    if (_currentDevice is not null && _currentDevice.Equals(incomingDevice)) return;

    _currentDevice = incomingDevice;
    var httpClient = _httpClient ??= new HttpClient();
    // TODO: Reflection
    if (_currentDevice is SmartProDevice device)
    {
      _deviceInfo = new SmartProDeviceControl
      {
        Device = device,
        HttpClient = httpClient
      };
      _deviceInfo.DeviceRemoved += RemoveDevice;
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

  private async void AddNewDevice(object? sender, IpPrompt.DeviceAddedEventArgs args)
  {
    var instance = Activator.CreateInstance(args.Type, args.IpAddress, args.Port, args.Id);
    if (instance is null) throw new Exception("Null instance returned from Activator.CreateInstance() call");
    var device = (Device)instance;
    if (Devices!.Contains(device))
    {
      MessageBox.Show("Urządzenie już istnieje");
      return;
    }

    await Database.AddDeviceAsync(device, args.Type);
    Devices.Add(device);
  }

  private async void RemoveDevice(object? sender, SmartProDeviceControl.RemoveDeviceEventArgs args)
  {
    await Database.RemoveDeviceAsync(args.Device);
    Devices!.Remove(args.Device);
    OpenDevices!.Remove(DeviceControls[args.Device]);
  }

  private void AddNewDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new()
    {
      Types = Types
    };
    prompt.DeviceAdded += AddNewDevice;
    prompt.Show();
  }
}