using System.Windows.Controls;

namespace PolEko;

public partial class DeviceInfoDisplay
{
  public DeviceInfoDisplay(Device device)
  {
    InitializeComponent();
    NameBlock.Text = device.ToString();
    IpBlock.Text = device.IpAddress.ToString();
    TypeBlock.Text = device.Type;
    DescriptionBlock.Text = device.Description;
    RefreshRateBlock.Text = device.RefreshRate.ToString();
  }
}