using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

  #region Fields
  
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
  
  /// <summary>
  /// Describes what types of Devices exist together with their string representation
  /// </summary>
  public Dictionary<string, Type>? Types
  {
    get => (Dictionary<string, Type>)GetValue(TypesProperty);
    init => SetValue(TypesProperty, value);
  }

  /// <summary>
  /// Describes what devices exist in the database and are therefore going to be displayed on the side panel
  /// </summary>
  public ObservableCollection<Device>? Devices
  {
    get => (ObservableCollection<Device>)GetValue(DevicesProperty);
    set => SetValue(DevicesProperty, value);
  }

  /// <summary>
  /// Describes what devices are currently open
  /// </summary>
  public ObservableCollection<TabItem>? OpenDevices
  {
    get => (ObservableCollection<TabItem>)GetValue(OpenDevicesProperty);
    set => SetValue(OpenDevicesProperty, value);
  }

  /// <summary>
  /// Describes a currently selected DeviceControl
  /// </summary>
  public TabItem? SelectedDeviceControl
  {
    get => (TabItem)GetValue(SelectedDeviceControlProperty);
    set => SetValue(SelectedDeviceControlProperty, value);
  }

  /// <summary>
  /// Describes a Device and a TabItem associated with it
  /// </summary>
  private Dictionary<Device, TabItem> DeviceControls { get; } = new();

  /// <summary>
  /// Describes Type of UserControl associated with a Type of device
  /// </summary>
  public Dictionary<Type, Type>? DeviceAssociatedControls { get; init; }
  
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
  
  #region Event handlers

  private void HandleDisplayedDeviceChange(object sender, RoutedEventArgs e)
  {
    // Check for potential invalid args
    if (sender is not ListBoxItem value)
      throw new ArgumentException("You can only use this method to handle ListBoxItem Click event");
    value.IsSelected = false;
    if (value.Content is not Device incomingDevice)
      throw new ArgumentException("ListBoxItem's content can only be of type Device");
    
    // Switch to device's tab if its control was already initialised
    if (DeviceControls.ContainsKey(incomingDevice))
    {
      SelectedDeviceControl = DeviceControls[incomingDevice];
      return;
    }

    var httpClient = _httpClient ??= new HttpClient();

    var t = incomingDevice.GetType();
    var instance =
      (IDeviceControl<Device>)Activator.CreateInstance(DeviceAssociatedControls![t], incomingDevice, httpClient)!;
    instance.DeviceRemoved += RemoveDevice;
    instance.DeviceClosed += delegate(object? _, DeviceRemovedEventArgs args)
    {
      // Find TabItem with Content property of control which wants to close
      var control = OpenDevices!.Where(x => x == DeviceControls[args.Device]).Select(x => x).FirstOrDefault();
      if (control is null) return;
      OpenDevices!.Remove(control);
      DeviceControls.Remove(args.Device);
      
      // If the closed device was the only one open, show the waiting screen, otherwise open the window which was opened
      // the latest
      if (OpenDevices.Count == 0)
      {
        IsDeviceOpen = false;
        return;
      }
      SelectedDeviceControl = OpenDevices[^1];
    };
    Closing += async delegate { await instance.DisposeAsync(); };
    _deviceInfo = instance;


    var formattedHeader = $"{incomingDevice.IpAddress}:{incomingDevice.Port}";
    var item = new TabItem
    {
      Content = _deviceInfo,
      Header = formattedHeader
    };

    DeviceControls[incomingDevice] = item;
    
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