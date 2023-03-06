using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace PolEko;

public partial class IpPrompt
{
  /// <summary>
  ///   Callback used to return the IP address to the caller
  /// </summary>
  private readonly Action<IPAddress> _callback;

  /// <summary>
  ///   IP address entered via the textbox in the <c>IpPrompt</c> prompt
  /// </summary>
  private IPAddress? _ip;

  public IpPrompt(Action<IPAddress> callback)
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
    if (IpRegex().IsMatch(IpTextBox.Text))
    {
      _ip = IPAddress.Parse(IpTextBox.Text);
      _callback(_ip);
      Close();
    }
    else
    {
      MessageBox.Show("Nieprawidłowy adres IP.");
    }
  }

  private void CancelButton_Click(object sender, RoutedEventArgs e)
  {
    Close();
  }


  /// <summary>
  ///   Regular expression used to parse IPv4 addresses
  /// </summary>
  /// <returns></returns>
  [GeneratedRegex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
  private static partial Regex IpRegex();
}