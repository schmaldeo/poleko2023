using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace PolEko.ui;

#pragma warning disable
public partial class ExampleDeviceControl : IDeviceControl<ExampleDevice>
{
  public ExampleDevice Device { get; }
  public HttpClient HttpClient { get; }

  public event PropertyChangedEventHandler? PropertyChanged;
  public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;
  
  public ExampleDeviceControl()
  {
    InitializeComponent();
  }

  public ExampleDeviceControl(ExampleDevice device, HttpClient client)
  {
    InitializeComponent();
    Device = device;
    HttpClient = client;
  }
  
  private void DeleteDevice_OnClick(object sender, RoutedEventArgs e)
  {
    DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(Device));
    Dispose();
  }

  public void Dispose(){}
  
  public async ValueTask DisposeAsync() {}
}
