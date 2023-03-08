using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PolEko;

public partial class IpPrompt
{
  /// <summary>
  ///   Callback used to return the IP address to the caller
  /// </summary>
  private readonly Action<IPAddress, int> _callback;

  /// <summary>
  ///   IP address entered via the textbox in the <c>IpPrompt</c> prompt
  /// </summary>
  private IPAddress? _ip;

  public IpPrompt(Action<IPAddress, int> callback)
  {
    InitializeComponent();
    IpTextBox.Focus();
    _callback = callback;
  }

  /// <summary>
  ///   Method that tries to parse the IP address entered in the prompt and either return it to the caller through the
  ///   callback or show a message box saying the IP is invalid
  /// </summary>
  /// <param name="sender"></param>
  /// <param name="e"></param>
  private void OkButton_Click(object sender, RoutedEventArgs e)
  {
    if (!Ipv4Regex().IsMatch(IpTextBox.Text))
    {
      MessageBox.Show("Nieprawidłowy adres IP");
      return;
    }
    
    if (!int.TryParse(PortTextBox.Text, out var port))
    {
      MessageBox.Show("Port nie może zawierać liter");
      return;
    }

    if (!Enumerable.Range(1, 65535).Contains(port))
    {
      MessageBox.Show("Port musi być z zakresu 1-65535");
      return;
    }

    
    _ip = IPAddress.Parse(IpTextBox.Text);
    _callback(_ip, port);
    Close();
  }

  private void CancelButton_Click(object sender, RoutedEventArgs e)
  {
    Close();
  }
  
  private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
  {
    var regex = NumericRegex();
    e.Handled = regex.IsMatch(e.Text);
  }
  // TODO: disable ability to paste non-numeric values
  
  /// <summary>
  ///   IPv4 regex
  /// </summary>
  /// <returns></returns>
  [GeneratedRegex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
  private static partial Regex Ipv4Regex();
  /// <summary>
  /// Number-only regex
  /// </summary>
  /// <returns></returns>
  [GeneratedRegex("[^0-9]+")]
  private static partial Regex NumericRegex();
}