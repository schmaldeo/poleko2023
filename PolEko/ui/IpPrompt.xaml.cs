using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace PolEko.ui;

public partial class IpPrompt
{
  #region DependencyProperties
  
  public static readonly DependencyProperty TypesProperty = MainWindow.TypesProperty;
  
  #endregion
  
  #region Constructors

  public IpPrompt()
  {
    InitializeComponent();
    IpTextBox.Focus();
    Loaded += delegate
    {
      if (Types is null) throw new ArgumentException("MainWindow must consume a Types property");
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
  
  #endregion
  
  #region Events

  public event EventHandler<DeviceAddedEventArgs>? DeviceAdded;
  
  #endregion
  
  #region Event handlers

  /// <summary>
  /// \~english Tries to parse entered parameters and raises the <see cref="DeviceAdded"/> or shows a <see cref="MessageBox"/>
  /// \~polish Sprawdza, czy wpisane parametry są poprawne i albo zgłasza zdarzenie <see cref="DeviceAdded"/>, albo pokazuje <see cref="MessageBox"/>
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="e"></param>
  private void OkButton_Click(object sender, RoutedEventArgs e)
  {
    if (DeviceAdded is null) return;

    // Check if IP input is a valid IPv4
    if (!Ipv4Regex().IsMatch(IpTextBox.Text))
    {
      var str = (string)Application.Current.FindResource("InvalidIp")!;
      MessageBox.Show(str);
      return;
    }

    // Check if port input can be parsed to an integer 
    if (!ushort.TryParse(PortTextBox.Text, out var port))
    {
      var str = (string)Application.Current.FindResource("PortFromRange")!;
      MessageBox.Show(str);
      return;
    }

    // As friendly name (referred to as ID) is optional, set it to null and only set a value to it if the input isn't empty
    string? id = null;
    if (IdTextBox.Text != string.Empty) id = IdTextBox.Text;

    var ip = IPAddress.Parse(IpTextBox.Text);

    // Selects the type from dictionary matching it with SelectedValue property on the ComboBox
    var type = Types!.Where(x => x.Value
      .Equals(TypesComboBox.SelectedValue)).Select(x => x.Value).First();

    DeviceAdded?.Invoke(this, new DeviceAddedEventArgs(ip, port, id, type));

    Close();
  }

  private void CancelButton_Click(object sender, RoutedEventArgs e)
  {
    Close();
  }

  /// <summary>
  ///   Command binding that disallows copying and pasting
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="e"></param>
  private void CommandBinding_CanExecutePaste(object sender, CanExecuteRoutedEventArgs e)
  {
    e.CanExecute = false;
    e.Handled = true;
  }
  
  #endregion

  #region Regex
  
  /// <summary>
  ///   IPv4 regex
  /// </summary>
  /// <returns></returns>
  [GeneratedRegex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
  private static partial Regex Ipv4Regex();
  
  #endregion

  public class DeviceAddedEventArgs : EventArgs
  {
    public DeviceAddedEventArgs(IPAddress ipAddress, ushort port, string? id, Type type)
    {
      IpAddress = ipAddress;
      Port = port;
      Id = id;
      Type = type;
    }

    public IPAddress IpAddress { get; }
    public ushort Port { get; }
    public string? Id { get; }
    public Type Type { get; }
  }
}

[ValueConversion(typeof(Dictionary<string, Type>), typeof(Dictionary<string, Type>))]
public class DeviceClassToModelConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is not Dictionary<string, Type> dict)
    {
      return new Dictionary<string, Type>();
    }
    var newDict = new Dictionary<string, Type>();
    foreach (var (_, type) in dict)
    {
      // If a device doesn't specify the DeviceModelAttribute, use type's name
      string name;
      try
      {
        var model = (DeviceModelAttribute)type.GetCustomAttributes(typeof(DeviceModelAttribute), false)[0];
        name = model.Model;
      }
      catch (IndexOutOfRangeException)
      {
        name = type.Name;
      }
      newDict[name] = type;
    }

    return newDict;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}