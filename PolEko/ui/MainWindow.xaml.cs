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

  #endregion

  #region Fields
  
  private IDeviceControl<Device>? _deviceInfo;
  private HttpClient? _httpClient;
  private TabItem? _selectedDeviceControl;
  private ObservableCollection<TabItem>? _openDevices;
  private bool _deviceOpen;

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
  
  #region Properties
  
  /// <summary>
  /// \~english <see cref="Dictionary{TKey,TValue}"/> of <see cref="Type"/>s of <see cref="Device{TMeasurement,TControl}"/> and the
  /// <c>Name</c> property of that Type as <c>TKey</c>
  /// \~polish Słownik typów urządzeń jako <c>TKey</c> i ich reprezentacja typu string
  /// </summary>
  public Dictionary<string, Type>? Types
  {
    get => (Dictionary<string, Type>)GetValue(TypesProperty);
    init => SetValue(TypesProperty, value);
  }

  /// <summary>
  /// \~english Describes what devices exist in the database and are therefore going to be displayed on the side panel
  /// \~polish Określa, jakie typy urządzeń istnieją w bazie danych. Te będą wyświetlone na bocznym panelu
  /// </summary>
  public ObservableCollection<Device>? Devices
  {
    get => (ObservableCollection<Device>)GetValue(DevicesProperty);
    set => SetValue(DevicesProperty, value);
  }

  /// <summary>
  /// \~english Describes what devices are currently open
  /// \~polish Określa, jakie urządzenia są obecnie otwarte
  /// </summary>
  public ObservableCollection<TabItem>? OpenDevices
  {
    get => _openDevices;
    private set
    {
      _openDevices = value;
      OnPropertyChanged();
    }
  }

  /// <summary>
  /// \~english Describes a currently selected DeviceControl
  /// \~polish Określa obecnie otwarte urządzenie
  /// </summary>
  public TabItem? SelectedDeviceControl
  {
    get => _selectedDeviceControl;
    set
    {
      _selectedDeviceControl = value;
      OnPropertyChanged();
    }
  }

  /// <summary>
  /// \~english Describes a Device and a TabItem associated with it
  /// \~polish Określa urządzenie <see cref="Device"/> i <see cref="TabItem"/> powiązany z nim
  /// </summary>
  private Dictionary<Device, TabItem> DeviceControls { get; } = new();

  /// <summary>
  /// \~english Describes Type of UserControl associated with a Type of device
  /// \~polish Określa typ UserControl powiązany z typem urządzenia
  /// </summary>
  public Dictionary<Type, Type>? DeviceAssociatedControls { get; init; }
  
  /// <summary>
  /// \~english Indicates whether any device is currently open
  /// \~polish Określa, czy jakiekolwiek urządzenie jest obecnie otwarte
  /// </summary>
  public bool DeviceOpen
  {
    get => _deviceOpen;
    set
    {
      _deviceOpen = value;
      OnPropertyChanged();
    }
  }
  
  #endregion

  #region Events
  
  public event PropertyChangedEventHandler? PropertyChanged;
  
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
    try
    {
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
          DeviceOpen = false;
          return;
        }

        SelectedDeviceControl = OpenDevices[^1];
      };
      Closing += async delegate { await instance.DisposeAsync(); };
      _deviceInfo = instance;
    }
    catch (MissingMethodException)
    {
      var type = t.BaseType!.GetGenericArguments()[1];
      throw new MissingMethodException($"{type} must implement a ({t}, HttpClient) constructor");
    }


    var formattedHeader = $"{incomingDevice.IpAddress}:{incomingDevice.Port}";
    var item = new TabItem
    {
      Content = _deviceInfo,
      Header = formattedHeader
    };

    DeviceControls[incomingDevice] = item;
    
    OpenDevices ??= new ObservableCollection<TabItem>();
    OpenDevices.Add(item);
    DeviceOpen = true;
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
      DeviceOpen = false;
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