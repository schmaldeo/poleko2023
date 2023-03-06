using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace PolEko;

public partial class IpPrompt
{
  private IPAddress? _ip;

  private void OkButton_Click(object sender, RoutedEventArgs e)
  {
    if (MyRegex().IsMatch(IpTextBox.Text))
    {
      _ip = IPAddress.Parse(IpTextBox.Text);
      MessageBox.Show(_ip.ToString());
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
  
  public IpPrompt()
  {
    InitializeComponent();
  }

  [GeneratedRegex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
  private static partial Regex MyRegex();
}