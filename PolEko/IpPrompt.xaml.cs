using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace PolEko;

public partial class IpPrompt
{
  public static readonly DependencyProperty CallbackProperty = 
    DependencyProperty.Register(nameof(Callback), typeof(Action<IPAddress, ushort, string?, Type>), typeof(IpPrompt));

  public Action<IPAddress, ushort, string?, Type>? Callback
  {
    get => (Action<IPAddress, ushort, string?, Type>)GetValue(CallbackProperty);
    set => SetValue(CallbackProperty, value);
  }
  
  public static readonly DependencyProperty TypesProperty = MainWindow.TypesProperty;

  public Dictionary<string, Type>? Types
  {
    get => (Dictionary<string, Type>)GetValue(TypesProperty);
    set => SetValue(TypesProperty, value);
  }

  public IpPrompt()
  {
    
    InitializeComponent();
    IpTextBox.Focus();
  }

  /// <summary>
  ///   Method that tries to parse the IP address entered in the prompt and either return it to the caller through the
  ///   callback or show a message box saying the IP is invalid
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="e"></param>
  private void OkButton_Click(object sender, RoutedEventArgs e)
  {
    if (Callback is null)
    {
      return;
    }

    // Check if IP input is a valid IPv4
    if (!Ipv4Regex().IsMatch(IpTextBox.Text))
    {
      MessageBox.Show("Nieprawidłowy adres IP");
      return;
    }

    // Check if port input can be parsed to an integer 
    if (!int.TryParse(PortTextBox.Text, out var port))
    {
      MessageBox.Show("Port może się składać tylko z liczb z zakresu 1-65535");
      return;
    }

    // Check if port input is from a valid range (unsigned 16 bit)
    if (!Enumerable.Range(1, 65535).Contains(port))
    {
      MessageBox.Show("Port musi być z zakresu 1-65535");
      return;
    }

    // As friendly name (referred to as ID) is optional, set it to null and only set a value to it if the input isn't empty
    string? id = null;
    if (IdTextBox.Text != string.Empty) id = IdTextBox.Text;

    var ip = IPAddress.Parse(IpTextBox.Text);

    var type = Types[TypesComboBox.Text];
    Callback(ip, (ushort)port, id, type);
    
    Close();
  }

  private void CancelButton_Click(object sender, RoutedEventArgs e)
  {
    Close();
  }

  /// <summary>
  /// Command binding that disallows copying and pasting
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="e"></param>
  private void CommandBinding_CanExecutePaste(object sender, CanExecuteRoutedEventArgs e)
  {
    e.CanExecute = false;
    e.Handled = true;
  }
  
  /// <summary>
  /// Event handler disallowing entering anything but numbers into an input
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="e"></param>
  private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
  {
    var regex = NumericRegex();
    e.Handled = regex.IsMatch(e.Text);
  }
  
  /// <summary>
  ///   IPv4 regex
  /// </summary>
  /// <returns></returns>
  [GeneratedRegex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
  private static partial Regex Ipv4Regex();

  /// <summary>
  ///   Number-only regex
  /// </summary>
  /// <returns></returns>
  [GeneratedRegex("[^0-9]+")]
  private static partial Regex NumericRegex();
}