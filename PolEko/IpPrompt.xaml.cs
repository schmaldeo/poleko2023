using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace PolEko;

public partial class IpPrompt
{
  private IPAddress? _ip;
  private readonly Action<IPAddress> _callback;

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
  
  [GeneratedRegex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
  private static partial Regex IpRegex();

  public IpPrompt(Action<IPAddress> callback)
  {
    InitializeComponent();
    _callback = callback;
  }
}
