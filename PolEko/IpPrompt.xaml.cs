using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace PolEko;

public partial class IpPrompt
{
  /// <summary>
  ///   Callback used to return the IP address to the caller
  /// </summary>
  private readonly Action<IPAddress, ushort, string?> _callback;

  public IpPrompt(Action<IPAddress, ushort, string?> callback)
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

    string? id = null;
    if (IdTextBox.Text != string.Empty) id = IdTextBox.Text;

    var ip = IPAddress.Parse(IpTextBox.Text);
    _callback(ip, (ushort)port, id);
    Close();
  }

  private void CancelButton_Click(object sender, RoutedEventArgs e)
  {
    Close();
  }

  private void CommandBinding_CanExecutePaste(object sender, CanExecuteRoutedEventArgs e)
  {
    e.CanExecute = false;
    e.Handled = true;
  }
  
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