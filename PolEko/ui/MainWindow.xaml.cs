using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using PolEko.util;

namespace PolEko.ui;

public partial class MainWindow : INotifyPropertyChanged
{
  #region DependencyProperties 
  
  public static readonly DependencyProperty TypesProperty =
    DependencyProperty.Register(nameof(Types), typeof(Dictionary<string, Type>), typeof(IpPrompt));

  public static readonly DependencyProperty DevicesProperty =
    DependencyProperty.Register(nameof(Devices), typeof(ObservableCollection<Device>), typeof(MainWindow));

  public static readonly DependencyProperty OpenDevicesProperty =
    DependencyProperty.Register(nameof(OpenDevices), typeof(ObservableCollection<TabItem>), typeof(MainWindow));

  public static readonly DependencyProperty SelectedDeviceControlProperty =
    DependencyProperty.Register(nameof(SelectedDeviceControl), typeof(TabItem), typeof(MainWindow));
  
  #endregion

  #region Properties

  public bool IsDeviceOpen
  {
    get => _isDeviceOpen;
    set
    {
      _isDeviceOpen = value;
      OnPropertyChanged();
    }
  }

  #endregion

  #region Fields
  
  private Device? _currentDevice;
  private IDeviceControl<Device>? _deviceInfo;
  private HttpClient? _httpClient;
  private bool _isDeviceOpen;

  #endregion

  #region Constructors
  
  public MainWindow()
  {
    InitializeComponent();
    Loaded += delegate
    {
      Devices ??= new ObservableCollection<Device>();
      if (DeviceAssociatedControls is null)
        throw new ArgumentException("You must pass DeviceAssociatedControls to MainWindow");
    };
  }
  
  #endregion

  #region Events
  
  public event PropertyChangedEventHandler? PropertyChanged;
  
  #endregion
  
  #region Properties
  
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

  public Dictionary<Type, Type>? DeviceAssociatedControls { get; init; }
  
  #endregion
  
  #region Event handlers

  private void HandleDisplayedDeviceChange(object sender, RoutedEventArgs e)
  {
    // Check for potential invalid args
    if (sender is not ListBoxItem value)
      throw new ArgumentException("You can only use this method to handle ListBoxItem Click event");
    if (value.Content is not Device incomingDevice)
      throw new ArgumentException("Button's content can only be of type Device");
    
    // Switch to device's tab if its control was already initialised
    if (DeviceControls.ContainsKey(incomingDevice))
    {
      SelectedDeviceControl = DeviceControls[incomingDevice];
      return;
    }

    // Disallow reopening a device that's currently open
    if (_currentDevice is not null && _currentDevice.Equals(incomingDevice)) return;
    
    var httpClient = _httpClient ??= new HttpClient();

    var t = incomingDevice.GetType();
    var instance =
      (IDeviceControl<Device>)Activator.CreateInstance(DeviceAssociatedControls![t], incomingDevice, httpClient)!;
    instance.DeviceRemoved += RemoveDevice;
    Closing += async delegate { await instance.DisposeAsync(); };
    _deviceInfo = instance;


    var formattedHeader = $"{incomingDevice.IpAddress}:{incomingDevice.Port}";
    var item = new TabItem
    {
      Content = _deviceInfo,
      Header = formattedHeader
    };

    DeviceControls[incomingDevice] = item;

    _currentDevice = incomingDevice;
    OpenDevices ??= new ObservableCollection<TabItem>();
    OpenDevices.Add(item);
    IsDeviceOpen = true;
    SelectedDeviceControl = item;
  }

  private async void AddNewDevice(object? sender, IpPrompt.DeviceAddedEventArgs args)
  {
    var instance = Activator.CreateInstance(args.Type, args.IpAddress, args.Port, args.Id);
    if (instance is null) throw new Exception("Null instance returned from Activator.CreateInstance() call");
    var device = (Device)instance;
    if (Devices!.Contains(device))
    {
      var str = (string)Application.Current.FindResource("DeviceAlreadyExists")!;
      MessageBox.Show(str);
      return;
    }

    await Database.AddDeviceAsync(device, args.Type);
    Devices.Add(device);
  }

  private async void RemoveDevice(object? sender, DeviceRemovedEventArgs args)
  {
    await Database.RemoveDeviceAsync(args.Device);
    Devices!.Remove(args.Device);
    OpenDevices!.Remove(DeviceControls[args.Device]);
    if (OpenDevices!.Count == 0)
    {
      IsDeviceOpen = false;
    }
  }

  private void AddNewDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new()
    {
      Types = Types,
      Owner = this
    };
    prompt.DeviceAdded += AddNewDevice;
    prompt.ShowDialog();
  }
  
  private void OnPropertyChanged([CallerMemberName] string? name = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
  
  #endregion
}