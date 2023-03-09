using System.Windows.Controls;

namespace PolEko;

public partial class DeviceInfoDisplay
{
  public DeviceInfoDisplay(Device device)
  {
    InitializeComponent();
    Label devInfo = new()
    {
      Content = device.ToString()
    };
    Grid.Children.Add(devInfo);
  }
}