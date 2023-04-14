using System.Net.Http;

namespace PolEko.ui;

public partial class ExampleDeviceControl
{
  public ExampleDeviceControl()
  {
    InitializeComponent();
  }

  public ExampleDeviceControl(ExampleDevice device, HttpClient client) : base(device, client)
  {
    InitializeComponent();
  }
}